import * as readline from "node:readline";

const rl = readline.createInterface({ input: process.stdin, terminal: false });

rl.on("line", (line) => {
  process.stdout.write(line.replaceAll("、", "，").replaceAll("。", "．") + "\n");
});
