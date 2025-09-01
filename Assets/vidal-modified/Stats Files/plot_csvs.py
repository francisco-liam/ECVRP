import os
import pandas as pd
import matplotlib.pyplot as plt
import argparse

def process_and_plot(subdirectory, bks_value):
    # Build full paths to the three expected files
    metrics_file = None
    avg_file = None
    min_file = None
    
    for f in os.listdir(subdirectory):
        if f.endswith(" - Metrics.csv"):
            metrics_file = os.path.join(subdirectory, f)
        elif f.endswith("-AvgFeasCost.csv"):
            avg_file = os.path.join(subdirectory, f)
        elif f.endswith("-MinFeasCost.csv"):
            min_file = os.path.join(subdirectory, f)

    if not (metrics_file and avg_file and min_file):
        print("Error: Could not find all required CSV files in", subdirectory)
        return

    # --- Process Metrics file ---
    try:
        df_metrics = pd.read_csv(metrics_file, header=None)
        avg_col2 = round(df_metrics.iloc[:, 2].mean(), 1)
        gap = round(((avg_col2 - bks_value) / bks_value) * 100, 3)
        avg_time = round(df_metrics.iloc[:, 4].mean(), 1)
        avg_tot_it = round(df_metrics.iloc[:, 5].mean(), 1)

        print(f"{subdirectory}:")
        print(f"Avg: {avg_col2}")
        print(f"Gap: {gap}%")
        print(f"AvgTime: {avg_time}")
        print(f"AvgTotIt: {avg_tot_it}")

    except Exception as e:
        print(f"Error reading Metrics file: {e}")
        return

    # --- Plot AvgFeasCost & MinFeasCost ---
    plt.figure(figsize=(10, 6))
    for path, label in [(avg_file, "AvgFeasCost"), (min_file, "MinFeasCost")]:
        try:
            df = pd.read_csv(path, header=None)
            data = df.to_numpy()
            plt.plot(data[:, 0], data[:, 1], label=label)
        except Exception as e:
            print(f"Error reading {label} file: {e}")

    # Plot BKS line
    plt.axhline(y=bks_value, color='red', linestyle='--', label=f"BKS: {bks_value}")

    plt.title("AvgFeasCost & MinFeasCost with BKS Line")
    plt.xlabel("X")
    plt.ylabel("Y")
    plt.legend()
    plt.grid(True)
    plt.tight_layout()

    # Save plot to PNG
    output_name = os.path.basename(os.path.normpath(subdirectory)) + ".png"
    output_path = os.path.join(subdirectory, output_name)
    plt.savefig(output_path)
    print(f"Plot saved as: {output_path}")

    # Show plot
    #plt.show()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Process Metrics CSV and plot Avg/Min Feas Cost with BKS")
    parser.add_argument("subdirectory", help="The subdirectory containing the CSV files")
    parser.add_argument("bks_value", type=float, help="The Y-value for the BKS line")

    args = parser.parse_args()
    process_and_plot(args.subdirectory, args.bks_value)
