"""
Mock data for fallback when API is unavailable.
"""

# Mock tutorials data
TUTORIALS = [
    {
        "id": "powershell-basics-1",
        "title": "PowerShell Basics - Part 1",
        "description": "Learn the fundamentals of PowerShell",
        "xp": 100,
        "steps": [
            {
                "id": 1,
                "instruction": "Print 'Hello World' using Write-Host",
                "command": "Write-Host 'Hello World'",
                "expectedOutput": "Hello World"
            },
            {
                "id": 2,
                "instruction": "List all files in the current directory",
                "command": "Get-ChildItem",
                "expectedOutput": ""
            }
        ]
    },
    {
        "id": "git-essentials",
        "title": "Git Essentials",
        "description": "Master the basics of Git version control",
        "xp": 150,
        "steps": [
            {
                "id": 1,
                "instruction": "Initialize a new Git repository",
                "command": "git init",
                "expectedOutput": "Initialized empty Git repository"
            }
        ]
    },
    {
        "id": "python-basics",
        "title": "Python Basics",
        "description": "Introduction to Python programming",
        "xp": 120,
        "steps": [
            {
                "id": 1,
                "instruction": "Print 'Hello World' using print()",
                "command": "print('Hello World')",
                "expectedOutput": "Hello World"
            },
            {
                "id": 2,
                "instruction": "Create a simple list and iterate through it",
                "command": "fruits = ['apple', 'banana', 'cherry']\nfor fruit in fruits:\n    print(fruit)",
                "expectedOutput": "apple\nbanana\ncherry"
            }
        ]
    }
]
