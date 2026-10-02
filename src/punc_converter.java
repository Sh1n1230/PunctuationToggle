import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;

public class punc_converter {
    public static void main(String[] args) {
        // OSごとの文字コードの違いを吸収するためUTF-8を指定
        try (BufferedReader br = new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8))) {
            String line;
            while ((line = br.readLine()) != null) {
                System.out.println(line.replace("、", "，").replace("。", "．"));
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}