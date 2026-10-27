# PowerShell のスクリプト（Windows/install.ps1・uninstall.ps1）の PSScriptAnalyzer の設定。
# CI の「コーディング規約」ジョブと、CONTRIBUTING の「書式の確認」で使う。
@{
    Severity     = @('Error', 'Warning')
    ExcludeRules = @(
        # インストールの結果を利用者に表示するためのスクリプトなので、出力のストリームではなく画面に書く Write-Host を使う
        'PSAvoidUsingWriteHost'
    )
}
