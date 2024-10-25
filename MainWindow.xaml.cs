using System;
using System.Collections.Generic; // Added for using Dictionary
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

        // Dictionary mapping segment indices to prize amounts
        private readonly Dictionary<int, int> segmentPrizes = new Dictionary<int, int>
        {
            { 0, 10000 }, { 1, 5 }, { 2, 5000 }, { 3, 5 },
            { 4, 50 }, { 5, 5 }, { 6, 1000 }, { 7, 5 },
            { 8, 50 }, { 9, 5 }, { 10, 5000 }, { 11, 5 },
            { 12, 50 }, { 13, 5 }, { 14, 1000 }, { 15, 5 },
            { 16, 50 }, { 17, 5 }, { 18, 5000 }, { 19, 5 }
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
            int prizeAmount = segmentPrizes[prizeIndex]; // Use the dictionary to get the prize amount

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
