using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WheelGame
{
    public partial class MainWindow : Window
    {
        private bool isSpinning = false;

        // Updated prize amounts corresponding to each of the 20 segments
        private readonly int[] prizeAmounts = new int[]
        {
            5, 10, 20, 50, 100, 200, 500, 1000, 5000, 10000,
            20000, 50000, 75000, 100000, 125000, 150000, 175000, 200000, 225000, 250000
        };

        public MainWindow()
        {
            InitializeComponent();
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

            // Randomly select a stop angle between 0-360 degrees
            int rawAngle = random.Next(0, 360);

            // Adjust the angle to align with the nearest segment's center
            double segmentSize = 18.0;  // 20 segments of 18 degrees each
            double stopAngle = Math.Round(rawAngle / segmentSize) * segmentSize;

            await SpinWheelWithTicks(stopAngle, tickPlayer);  // Spin the wheel

            // Determine the prize based on the aligned stop angle.
            int prizeIndex = DeterminePrizeIndexFromAngle(stopAngle);
            int prizeAmount = prizeAmounts[prizeIndex];

            PrizeDisplay.Text = $"You won R{prizeAmount}!";

            SpinButton.IsEnabled = true;
            isSpinning = false;
        }

        // Spins the wheel and synchronizes tick sounds with segment crossings
        private async Task SpinWheelWithTicks(double stopAngle, MediaPlayer tickPlayer)
        {
            double fullRotations = 5;  // 5 full rotations
            double totalRotation = 360 * fullRotations + stopAngle;  // Total rotation angle
            double segmentSize = 18.0;  // 20 segments, each 18 degrees
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
                var clock = (AnimationClock)s;
                double? fromValue = spinAnimation.From;
                double? toValue = spinAnimation.To;
                double? progress = clock.CurrentProgress;

                if (fromValue.HasValue && toValue.HasValue && progress.HasValue)
                {
                    // Calculate the current angle based on animation progress
                    currentAngle = fromValue.Value + ((toValue.Value - fromValue.Value) * progress.Value);
                    currentAngle = (currentAngle % 360 + 360) % 360;  // Normalize angle between 0-360

                    int currentSegment = (int)(currentAngle / segmentSize);  // Calculate segment

                    if (currentSegment != lastSegment)  // Play tick if new segment is crossed
                    {
                        tickPlayer.Stop();
                        tickPlayer.Play();
                        lastSegment = currentSegment;
                    }
                }
            };

            WheelRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);
            await Task.Delay(spinAnimation.Duration.TimeSpan);
        }

        // Determines the prize index based on the final stop angle.
        private int DeterminePrizeIndexFromAngle(double finalAngle)
        {
            // Normalize the angle to a value between 0 and 360 degrees.
            finalAngle = (finalAngle % 360 + 360) % 360;

            // Each segment covers 18 degrees. Determine the segment index.
            int segmentIndex = (int)(finalAngle / 18.0) % 20;  // Ensure index is within range 0-19

            return segmentIndex;
        }
    }
}
