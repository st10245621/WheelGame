using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WheelGame
{
    public partial class MainWindow : Window
    {
        private int selectedPrizePrediction = 0;  // Player's selected prize prediction
        private bool isSpinning = false;

        // Corrected prize amounts corresponding to the 20 segments
        private readonly int[] prizeAmounts = new int[]
        {
        10000, 5, 5000, 5, 50, 5, 1000, 5,  // First 8 segments
        50, 5, 5000, 5, 50, 5, 1000, 5,     // Next 8 segments
        50, 5, 5000, 5                       // Last 4 segments before returning to red
        };


        public MainWindow()
        {
            InitializeComponent();
        }

        // Handle the player's prize prediction selection
        private void PrizePredictionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PrizePredictionComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Content?.ToString(), out int prediction))
            {
                selectedPrizePrediction = prediction;
                SpinButton.IsEnabled = true;  // Enable the Spin button
            }
            else
            {
                selectedPrizePrediction = 0;
                SpinButton.IsEnabled = false;  // Disable the Spin button
            }
        }

        // Spin button click handler
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
            double segmentSize = 18.0;  // 20 segments, each 18 degrees
            int rawAngle = random.Next(0, 360);
            double stopAngle = Math.Round(rawAngle / segmentSize) * segmentSize;

            await SpinWheelWithTicks(stopAngle, tickPlayer);  // Spin the wheel

            int prizeIndex = DeterminePrizeIndexFromAngle(stopAngle);
            int prizeAmount = prizeAmounts[prizeIndex];

            // Debugging: Show which segment and prize was selected
            MessageBox.Show($"Segment Index: {prizeIndex}, Prize: R{prizeAmount}");

            int winnings = prizeAmount;
            if (prizeAmount == selectedPrizePrediction)
            {
                winnings *= 2;  // Double the prize for correct prediction
                PrizeDisplay.Text = $"Congratulations! You predicted correctly and won 2x R{prizeAmount} = R{winnings}!";
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

        private int DeterminePrizeIndexFromAngle(double finalAngle)
        {
            finalAngle = (finalAngle % 360 + 360) % 360;
            int segmentIndex = (int)(finalAngle / 18.0) % 20;
            return segmentIndex;
        }
    }
}
