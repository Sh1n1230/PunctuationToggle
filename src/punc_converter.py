import sys

def main():
    for line in sys.stdin:
        sys.stdout.write(line.replace("、", "，").replace("。", "．"))

if __name__ == "__main__":
    main()
