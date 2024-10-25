using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WheelGame
{
    public enum Prize
    {
        RedR10000 = 10000,
        GreenR5 = 5,
        PurpleR1000 = 1000,
        BlueR50 = 50,
        YellowR5000 = 5000
    }

    public class GameController
    {
        private readonly Dictionary<int, Prize> segmentPrizes;
        private int selectedPrizePrediction;

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

        public void SetPrediction(int prediction)
        {
            selectedPrizePrediction = prediction;
        }

        public (int prizeAmount, bool isWin) SpinWheel(double stopAngle)
        {
            int prizeIndex = DeterminePrizeIndexFromAngle(stopAngle);
            int prizeAmount = (int)segmentPrizes[prizeIndex];
            bool isWin = prizeAmount == selectedPrizePrediction;

            return (prizeAmount, isWin);
        }

        private int DeterminePrizeIndexFromAngle(double finalAngle)
        {
            finalAngle = (finalAngle % 360 + 360) % 360;
            return (int)(finalAngle / 18.0) % 20;
        }
    }

    public partial class MainWindow : Window
    {
        private int selectedPrizePrediction = 0;
        private bool isSpinning = false;
        private GameController gameController;

        public MainWindow()
        {
            InitializeComponent();
            gameController = new GameController();
        }

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

        private async void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSpinning) return;
            isSpinning = true;
            SpinButton.IsEnabled = false;

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

            Random random = new Random();
            double segmentSize = 18.0;
            int rawAngle = random.Next(0, 360);
            double stopAngle = Math.Round(rawAngle / segmentSize) * segmentSize;

            await SpinWheelWithTicks(stopAngle, tickPlayer);

            (int prizeAmount, bool isWin) = gameController.SpinWheel(stopAngle);

            int winnings = prizeAmount;
            if (isWin)
            {
                winnings *= 2;
                PrizeDisplay.Text = $"Congratulations! You predicted correctly and won 2x R{prizeAmount} = R{winnings}!";
                ShowConfetti();  // Trigger confetti on win
            }
            else
            {
                PrizeDisplay.Text = $"The wheel landed on R{prizeAmount}. You won R{winnings}!";
            }

            SpinButton.IsEnabled = true;
            isSpinning = false;
        }

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

        // Method to Show Confetti
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

                DoubleAnimation fallAnimation = new DoubleAnimation
                {
                    From = -10,
                    To = ConfettiCanvas.ActualHeight + 10,
                    Duration = new Duration(TimeSpan.FromSeconds(random.Next(2, 5))),
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseInOut }
                };

                Storyboard.SetTarget(fallAnimation, confetti);
                Storyboard.SetTargetProperty(fallAnimation, new PropertyPath("(Canvas.Top)"));

                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(fallAnimation);
                storyboard.Begin();
            }
        }
    }
}
