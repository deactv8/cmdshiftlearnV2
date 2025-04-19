# PowerShell Basics: Part 3

## Working with Files and Directories

### Getting File Information
```powershell
Get-ChildItem -Path C:\Windows -Filter *.exe -Recurse -ErrorAction SilentlyContinue | Select-Object -First 10
```

### Creating Files and Directories
```powershell
# Create a new directory
New-Item -Path "C:\Temp\TestFolder" -ItemType Directory -Force

# Create a new file
New-Item -Path "C:\Temp\TestFolder\test.txt" -ItemType File -Value "Hello, PowerShell!" -Force
```

### Reading and Writing Files
```powershell
# Read a file
$content = Get-Content -Path "C:\Temp\TestFolder\test.txt"
Write-Host "File content: $content"

# Append to a file
Add-Content -Path "C:\Temp\TestFolder\test.txt" -Value "This is a new line."

# Overwrite a file
Set-Content -Path "C:\Temp\TestFolder\test.txt" -Value "This is new content."
```

## Working with CSV and JSON

### CSV Files
```powershell
# Create a CSV file
$data = @(
    [PSCustomObject]@{Name = "John"; Age = 30; City = "New York"}
    [PSCustomObject]@{Name = "Jane"; Age = 25; City = "Los Angeles"}
    [PSCustomObject]@{Name = "Bob"; Age = 40; City = "Chicago"}
)

$data | Export-Csv -Path "C:\Temp\TestFolder\people.csv" -NoTypeInformation

# Read a CSV file
$importedData = Import-Csv -Path "C:\Temp\TestFolder\people.csv"
$importedData | Format-Table
```

### JSON Files
```powershell
# Create a JSON file
$data | ConvertTo-Json | Set-Content -Path "C:\Temp\TestFolder\people.json"

# Read a JSON file
$jsonData = Get-Content -Path "C:\Temp\TestFolder\people.json" | ConvertFrom-Json
$jsonData | Format-Table
```

## Try It Yourself
1. Create a directory and a text file with some content
2. Read the content of the file and display it
3. Create a CSV file with information about your favorite books or movies
4. Read the CSV file and display its contents