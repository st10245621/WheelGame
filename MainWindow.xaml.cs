using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Policy;

namespace WheelGame
{
    // Defines the prizes available in the wheel game, each linked to a specific color segment and cash amount.
    public enum Prize
    {
        RedR10000 = 10000,
        GreenR5 = 5,
        PurpleR1000 = 1000,
        BlueR50 = 50,
        YellowR5000 = 5000
    }

    // Manages the game logic, including storing prize segments, setting user predictions,
    // and determining the outcome of a wheel spin based on where the wheel stops.
    public class GameController
    {
        private readonly Dictionary<int, Prize> segmentPrizes;
        private int selectedPrizePrediction;
        private static readonly Random random = new Random();


        // Initializes the GameController and sets up the prize segments for the wheel.
        public GameController()
        {
            segmentPrizes = new Dictionary<int, Prize>
            {
                { 0, Prize.RedR10000 }, { 1, Prize.GreenR5 }, { 2, Prize.PurpleR1000 }, { 3, Prize.GreenR5 },
                { 4, Prize.BlueR50 }, { 5, Prize.GreenR5 }, { 6, Prize.YellowR5000 }, { 7, Prize.GreenR5 },
                { 8, Prize.BlueR50 }, { 9, Prize.GreenR5 }, { 10, Prize.PurpleR1000 }, { 11, Prize.GreenR5 },
                { 12, Prize.BlueR50 }, { 13, Prize.GreenR5 }, { 14, Prize.YellowR5000 }, { 15, Prize.GreenR5 },
                { 16, Prize.BlueR50 }, { 17, Prize.GreenR5 }, { 18, Prize.PurpleR1000 }, { 19, Prize.GreenR5 }
            };
        }

        // Stores the user's selected prize prediction for later comparison.
        public void SetPrediction(int prediction)
        {
            selectedPrizePrediction = prediction;
        }

        // Generates a random stop angle for the wheel, aligned to the segment size.
        public double GenerateRandomAngle()
        {
            double segmentSize = 18.0;  // 360 degrees / 20 segments = 18 degrees per segment
            int rawAngle = random.Next(0, 360);  // Random angle between 0 and 359
            return Math.Round(rawAngle / segmentSize) * segmentSize;  // Align to the nearest segment
        }


        // Determines the prize amount and whether the user's prediction was correct
        // based on the final stop angle of the wheel.
       
        public (int prizeAmount, bool isWin) SpinWheel(double stopAngle)
        {
            int prizeIndex = DeterminePrizeIndexFromAngle(stopAngle);
            int prizeAmount = (int)segmentPrizes[prizeIndex];
            bool isWin = prizeAmount == selectedPrizePrediction;

            return (prizeAmount, isWin);
        }

        // Converts the final angle of the wheel to a segment index, ensuring
        // that the angle wraps correctly and corresponds to the appropriate segment.
        private int DeterminePrizeIndexFromAngle(double finalAngle)
        {
            finalAngle = (finalAngle % 360 + 360) % 360;
            return (int)(finalAngle / 18.0) % 20;
        }
    }

    // Initializes the MainWindow and sets up the game controller.
    // This constructor ensures the UI components are loaded and prepares the game logic.
    public partial class MainWindow : Window
    {
        private int selectedPrizePrediction = 0;
        private bool isSpinning = false;
        private GameController gameController;
        private int spinCount = 0;  
        private int totalWinnings = 0;  

        public MainWindow()
        {
            InitializeComponent();
            gameController = new GameController();
        }

        // Handles the selection change in the prize prediction ComboBox.
        // If the user selects a valid amount, it enables the spin button and stores the prediction.
        // If no valid selection is made, it disables the spin button.
        private void PrizePredictionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PrizePredictionComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Content?.ToString(), out int prediction))
            {
                selectedPrizePrediction = prediction;
                SpinButton.IsEnabled = true;
                gameController.SetPrediction(prediction);
            }
            else
            {
                selectedPrizePrediction = 0;
                SpinButton.IsEnabled = false;
            }
        }

        // Handles the spin button click, initiating the wheel spin and displaying the result.
        // Disables the button during the spin to prevent repeated clicks. Plays a tick sound as the wheel spins,
        // calculates the result based on where the wheel stops, and shows a relevant message for jackpot or winnings.
        private async void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSpinning) return;
            isSpinning = true;
            SpinButton.IsEnabled = false;

            spinCount++;  // Increment spin count
            SpinCountDisplay.Text = $"Spins: {spinCount}";  // Update spin count display

            var tickPlayer = new MediaPlayer();
            string tickSoundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Tick.wav");

            if (!System.IO.File.Exists(tickSoundPath))
            {
                MessageBox.Show($"Tick.wav not found at: {tickSoundPath}");
                SpinButton.IsEnabled = true;
                isSpinning = false;
                return;
            }

            tickPlayer.Open(new Uri(tickSoundPath, UriKind.Absolute));

            // Use the GenerateRandomAngle method to get the stop angle.
            double stopAngle = gameController.GenerateRandomAngle();

            await SpinWheelWithTicks(stopAngle, tickPlayer);

            (int prizeAmount, bool isWin) = gameController.SpinWheel(stopAngle);
            int winnings = prizeAmount;

            if (prizeAmount == (int)Prize.RedR10000)
            {
                PrizeDisplay.Text = $"Jackpot! You won R{prizeAmount}!";
                ShowConfetti();
            }
            else if (isWin)
            {
                winnings *= 2;
                PrizeDisplay.Text = $"Congratulations! You predicted correctly and won 2x R{prizeAmount} = R{winnings}!";
                ShowConfetti();
            }
            else
            {
                PrizeDisplay.Text = $"The wheel landed on R{prizeAmount}. You won R{winnings}!";
            }

            totalWinnings += winnings;  // Accumulate winnings
            TotalWinningsDisplay.Text = $"Total Winnings: R{totalWinnings}";  // Update UI with total winnings

            SpinButton.IsEnabled = true;
            isSpinning = false;
        }



        // Animates the wheel spin with smooth deceleration, rotating for multiple full turns 
        // and stopping at a specified angle. A tick sound plays each time the wheel crosses into 
        // a new segment, enhancing the visual feedback for the user.
        private async Task SpinWheelWithTicks(double stopAngle, MediaPlayer tickPlayer)
        {
            double fullRotations = 5;
            double totalRotation = 360 * fullRotations + stopAngle;
            double segmentSize = 18.0;
            int lastSegment = -1;

            DoubleAnimation spinAnimation = new DoubleAnimation
            {
                From = 0,
                To = totalRotation,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);

            EventHandler renderingHandler = (s, e) =>
            {
                double currentAngle = (WheelRotation.Angle % 360 + 360) % 360;
                int currentSegment = (int)(currentAngle / segmentSize);

                if (currentSegment != lastSegment)
                {
                    tickPlayer.Stop();
                    tickPlayer.Play();
                    lastSegment = currentSegment;
                }
            };

            CompositionTarget.Rendering += renderingHandler;
            await Task.Delay(spinAnimation.Duration.TimeSpan);
            CompositionTarget.Rendering -= renderingHandler;
        }

        // Animates colorful, random-sized confetti falling smoothly from the top
        // only if the predicted amount matches the wheel's outcome.
        // Confetti pieces start off-screen, fall with varied speeds, and use easing for natural motion.
        private void ShowConfetti()
        {
            Random random = new Random();
            for (int i = 0; i < 50; i++)
            {
                
                Ellipse confetti = new Ellipse
                {
                    Width = random.Next(5, 10),  
                    Height = random.Next(5, 10),  
                    Fill = new SolidColorBrush(Color.FromRgb(
                        (byte)random.Next(256),
                        (byte)random.Next(256),
                        (byte)random.Next(256)))  
                };

                
                double startX = random.Next(0, (int)ConfettiCanvas.ActualWidth);
                Canvas.SetLeft(confetti, startX);
                Canvas.SetTop(confetti, -10);  

             
                ConfettiCanvas.Children.Add(confetti);

                
                double durationInSeconds = random.Next(1500, 3500) / 1000.0;

                
                DoubleAnimation fallAnimation = new DoubleAnimation
                {
                    From = -10,  
                    To = ConfettiCanvas.ActualHeight + 10,  
                    Duration = new Duration(TimeSpan.FromSeconds(durationInSeconds)),  
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }  
                };

          
                Storyboard.SetTarget(fallAnimation, confetti);
                Storyboard.SetTargetProperty(fallAnimation, new PropertyPath("(Canvas.Top)"));

                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(fallAnimation);
                storyboard.Begin();
            }
        }

        // Handles the Reset button click event to reset the game state.
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            
            spinCount = 0;
            totalWinnings = 0;

            
            SpinCountDisplay.Text = "Spins: 0";
            TotalWinningsDisplay.Text = "Total Winnings: R0";
            PrizeDisplay.Text = "Make a prediction and spin the wheel!";

            
            if (selectedPrizePrediction != 0)
            {
                SpinButton.IsEnabled = true;
            }

            ConfettiCanvas.Children.Clear();

            WheelRotation.Angle = 0;
        }
    }
}
