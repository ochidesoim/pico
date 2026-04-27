$future = (Get-Date).AddYears(1)
Get-ChildItem 'D:\pico\output\bracket_dep*.msh' | ForEach-Object {
    $_.LastWriteTime = $future
    Write-Host "Touched: $($_.Name)"
}

Remove-Item 'D:\pico\output\bracket_dep*.inp' -ErrorAction SilentlyContinue
Remove-Item 'D:\pico\output\bracket_dep*.frd' -ErrorAction SilentlyContinue
Remove-Item 'D:\pico\output\bracket_dep*.vtu' -ErrorAction SilentlyContinue
Remove-Item 'D:\pico\output\bracket_dep*.cvg' -ErrorAction SilentlyContinue
Remove-Item 'D:\pico\output\bracket_dep*.dat' -ErrorAction SilentlyContinue
Remove-Item 'D:\pico\output\bracket_dep*.sta' -ErrorAction SilentlyContinue
Remove-Item 'D:\pico\output\bracket_dep*.12d' -ErrorAction SilentlyContinue

Write-Host "Old FEA results cleared. Pipeline will re-solve with corrected BCs."
