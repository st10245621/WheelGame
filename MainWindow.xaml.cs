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
            int segmentIndex = random.Next(0, 8);
            int stopAngle = segmentIndex * 45 + 22;

            await SpinWheelWithTicks(stopAngle, tickPlayer);

            string prize = DeterminePrize(segmentIndex);
            PrizeDisplay.Text = $"You won {selectedPrizeAmount} with {prize}!";

            SpinButton.IsEnabled = true;
            isSpinning = false;
        }

        // Spins the wheel and synchronizes tick sounds with segment crossings
        private async Task SpinWheelWithTicks(int stopAngle, MediaPlayer tickPlayer)
        {
            int totalRotation = 360 * 5 + stopAngle;  // 5 full rotations + precise stop
            int segmentSize = 45;  // 8 segments, each 45 degrees
            double currentAngle = 0;

            // Animation setup
            DoubleAnimation spinAnimation = new DoubleAnimation
            {
                From = 0,
                To = totalRotation,
                Duration = new Duration(TimeSpan.FromSeconds(3)),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            // Track the last ticked segment to avoid repeated ticks
            int lastSegment = -1;
            bool finalTickPlayed = false;

            // Subscribe to the wheel rotation updates
            spinAnimation.CurrentTimeInvalidated += (s, e) =>
            {
                if (WheelRotation.Angle != currentAngle)  // If angle has changed
                {
                    currentAngle = WheelRotation.Angle % 360;  // Normalize between 0-360 degrees
                    int currentSegment = (int)(currentAngle / segmentSize);  // Determine the current segment

                    if (currentSegment != lastSegment)  // If a new segment is crossed
                    {
                        tickPlayer.Stop();  // Stop any previously playing sound
                        tickPlayer.Play();  // Play the tick sound
                        lastSegment = currentSegment;
                    }
                }

                // Play one last tick right before the animation completes
                if (!finalTickPlayed && spinAnimation.Duration.HasTimeSpan &&
                    spinAnimation.Duration.TimeSpan - TimeSpan.FromMilliseconds(100) <= (s as Clock)?.CurrentTime)
                {
                    tickPlayer.Stop();  // Stop any previously playing sound
                    tickPlayer.Play();  // Play the final tick sound
                    finalTickPlayed = true;  // Ensure final tick is played only once
                }
            };

            // Start the wheel animation
            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);

            // Wait for the animation to complete
            await Task.Delay(spinAnimation.Duration.TimeSpan);
        }


        // Determines the prize based on the final segment
        private string DeterminePrize(int segmentIndex)
        {
            return segmentIndex switch
            {
                0 => "Green",
                1 => "Blue",
                2 => "Purple",
                3 => "Yellow",
                4 => "Red",
                5 => "Orange",
                6 => "Light Green",
                7 => "Light Blue",
                _ => "Unknown"
            };
        }
    }
}
