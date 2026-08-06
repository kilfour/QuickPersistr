# Delete the .quickpulse directory
Remove-Item ".quickpulse" -Recurse -Force -ErrorAction SilentlyContinue
# Delete the .test-results directory
Remove-Item ".test-results" -Recurse -Force -ErrorAction SilentlyContinue
# Delete the temp md files
Remove-Item "temp-*.md" -Recurse -Force -ErrorAction SilentlyContinue
# Recursively delete all .log files 
Get-ChildItem -Path . -Filter *.log -Recurse -Force | Remove-Item -Force -ErrorAction SilentlyContinue
# Recursively delete all .quickcheckr directories
Get-ChildItem -Path . -Filter ".quickcheckr" -Recurse -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue