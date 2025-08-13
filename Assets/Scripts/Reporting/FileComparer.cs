using System;
using System.IO;
using System.Text;

public static class FileComparer
{
    // Compares files byte-by-byte
    public static bool AreFilesEqualBinary(string filePath1, string filePath2)
    {
        const int bufferSize = 8192;

        if (!File.Exists(filePath1) || !File.Exists(filePath2))
            return false;

        FileInfo fi1 = new FileInfo(filePath1);
        FileInfo fi2 = new FileInfo(filePath2);

        if (fi1.Length != fi2.Length)
            return false;

        using (FileStream fs1 = File.OpenRead(filePath1))
        using (FileStream fs2 = File.OpenRead(filePath2))
        {
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];

            int bytesRead1, bytesRead2;
            while ((bytesRead1 = fs1.Read(buffer1, 0, bufferSize)) > 0)
            {
                bytesRead2 = fs2.Read(buffer2, 0, bufferSize);
                if (bytesRead1 != bytesRead2)
                    return false;

                for (int i = 0; i < bytesRead1; i++)
                    if (buffer1[i] != buffer2[i])
                        return false;
            }
        }

        return true;
    }

    // Compares files line-by-line as text, with optional trimming
    public static bool AreFilesEqualText(string filePath1, string filePath2, bool trimLines = false)
    {
        if (!File.Exists(filePath1) || !File.Exists(filePath2))
            return false;

        using (var reader1 = new StreamReader(filePath1))
        using (var reader2 = new StreamReader(filePath2))
        {
            while (!reader1.EndOfStream || !reader2.EndOfStream)
            {
                string? line1 = reader1.ReadLine();
                string? line2 = reader2.ReadLine();

                if (line1 == null || line2 == null)
                    return false;

                if (trimLines)
                {
                    line1 = line1.Trim();
                    line2 = line2.Trim();
                }

                if (!string.Equals(line1, line2, StringComparison.Ordinal))
                    return false;
            }
        }

        return true;
    }

    public static int FindFirstDifference(string file1, string file2)
    {
        using (var reader1 = new StreamReader(file1))
        using (var reader2 = new StreamReader(file2))
        {
            int lineNumber = 1;
            string line1, line2;

            while ((line1 = reader1.ReadLine()) != null & (line2 = reader2.ReadLine()) != null)
            {
                if (line1 != line2)
                    return lineNumber;
                lineNumber++;
            }

            // Check if one file has extra lines
            if (reader1.ReadLine() != null || reader2.ReadLine() != null)
                return lineNumber;

            return -1; // Files are identical
        }
    }
}
