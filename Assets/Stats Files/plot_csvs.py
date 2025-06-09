import os
import pandas as pd
import matplotlib.pyplot as plt
import argparse

def plot_csvs(subdirectory, files):
    full_paths = [os.path.join(subdirectory, f) for f in files]

    plt.figure(figsize=(10, 6))
    
    for path, name in zip(full_paths, files):
        try:
            df = pd.read_csv(path)
            plt.plot(df.iloc[:, 0], df.iloc[:, 1], label=name)
        except Exception as e:
            print(f"Error reading {name}: {e}")

    plt.title("Combined Plot of CSV Files")
    plt.xlabel("X")
    plt.ylabel("Y")
    plt.legend()
    plt.grid(True)
    plt.tight_layout()
    plt.show()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Plot 3 CSV files from a subdirectory")
    parser.add_argument("subdirectory", help="The subdirectory containing the CSV files")
    parser.add_argument("file1", help="First CSV file name")
    parser.add_argument("file2", help="Second CSV file name")
    parser.add_argument("file3", help="Third CSV file name")

    args = parser.parse_args()
    plot_csvs(args.subdirectory, [args.file1, args.file2, args.file3])
