using System;

public class ProgressBar {
    private const int blockCount = 30;

    public static string GetProcessBar(double percentage) {
        if (percentage <= 0f) { return string.Format("[{0}]", new string('-', blockCount)); }
        if (percentage >= 1f) { return string.Format("[{0}]", new string('#', blockCount)); }
        var progressBlockCount = (int)MathF.Round(blockCount * (float)percentage);
        return string.Format("[{0}{1}]", new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount));
    }
}