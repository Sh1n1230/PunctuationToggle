use std::io::{self, BufRead, BufWriter, Write};

fn main() {
    let stdin = io::stdin();
    let stdout = io::stdout();
    let mut out = BufWriter::new(stdout.lock());

    for line in stdin.lock().lines() {
        let line = line.expect("failed to read line");
        let converted = line.replace('、', "，").replace('。', "．");
        writeln!(out, "{}", converted).expect("failed to write line");
    }
}
