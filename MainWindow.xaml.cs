using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WheelGame
{
    public partial class MainWindow : Window
    {
        private int selectedPrizeAmount = 0;
        private bool isSpinning = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Handle the prize amount selection
        private void PrizeAmountComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PrizeAmountComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Content?.ToString(), out int prizeAmount))
                {
                    selectedPrizeAmount = prizeAmount;
                }
                else
                {
                    selectedPrizeAmount = 0;
                    MessageBox.Show("Invalid prize amount selected.");
                }
            }
        }

        // Spin button click handler
        private async void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSpinning) return;  // Prevent multiple clicks during spin
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
            int rawAngle = random.Next(0, 360);  // Get a random stop angle between 0-360

            // Adjust the angle to align with the nearest segment's center
            int segmentSize = 30;  // 12 segments of 30 degrees each
            int stopAngle = (int)(Math.Round(rawAngle / (double)segmentSize) * segmentSize);

            await SpinWheelWithTicks(stopAngle, tickPlayer);  // Spin the wheel

            // Determine the prize based on the aligned stop angle.
            string prize = DeterminePrizeFromAngle(stopAngle);
            PrizeDisplay.Text = $"You won {selectedPrizeAmount} with {prize}!";

            SpinButton.IsEnabled = true;
            isSpinning = false;
        }

        // Spins the wheel and synchronizes tick sounds with segment crossings
        private async Task SpinWheelWithTicks(int stopAngle, MediaPlayer tickPlayer)
        {
            int totalRotation = 360 * 5 + stopAngle;  // 5 full rotations + precise stop
            int segmentSize = 30;  // 12 segments, each 30 degrees
            double currentAngle = 0;
            int lastSegment = -1;  // Track the last ticked segment

            DoubleAnimation spinAnimation = new DoubleAnimation
            {
                From = 0,
                To = totalRotation,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            spinAnimation.CurrentTimeInvalidated += (s, e) =>
            {
                currentAngle = (WheelRotation.Angle % 360 + 360) % 360;  // Normalize angle
                int currentSegment = (int)(currentAngle / segmentSize);  // Calculate segment

                if (currentSegment != lastSegment)  // Play tick if new segment is crossed
                {
                    tickPlayer.Stop();
                    tickPlayer.Play();
                    lastSegment = currentSegment;
                }
            };

            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);
            await Task.Delay(spinAnimation.Duration.TimeSpan);
        }

        // Determines the prize based on the final stop angle.
        private string DeterminePrizeFromAngle(double finalAngle)
        {
            // Normalize the angle to a value between 0 and 360 degrees.
            finalAngle = (finalAngle % 360 + 360) % 360;

            // Each segment covers 30 degrees. Determine the segment index.
            int segmentIndex = (int)(finalAngle / 30);

            // Map the segment index to the correct prize color.
            return segmentIndex switch
            {
                0 => "Red",
                1 => "Purple",
                2 => "Green",
                3 => "Blue",
                4 => "Yellow",
                5 => "Green",
                6 => "Purple",
                7 => "Blue",
                8 => "Yellow",
                9 => "Red",
                10 => "Purple",
                11 => "Green",
                _ => "Unknown"
            };
        }
    }
}
