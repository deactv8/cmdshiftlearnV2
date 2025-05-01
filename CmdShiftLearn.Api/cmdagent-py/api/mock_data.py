"""
Mock data for fallback when API is unavailable.
"""

# Mock tutorials data
TUTORIALS = [
    {
        "id": "powershell-basics-1",
        "title": "PowerShell Basics - Part 1",
        "description": "Learn the fundamentals of PowerShell including basic commands and syntax.",
        "difficulty": "Beginner",
        "steps": [
            {
                "id": 1,
                "instructions": "Let's start with the classic 'Hello World'. In PowerShell, you can print text to the console using the Write-Host cmdlet.\n\nTry it now by typing: `Write-Host \"Hello, PowerShell!\"`",
                "expectedCommand": "Write-Host \"Hello, PowerShell!\"",
                "hint": "Use the Write-Host cmdlet followed by text in quotes"
            },
            {
                "id": 2,
                "instructions": "Now let's list all files in the current directory. In PowerShell, you can use the Get-ChildItem cmdlet (which has aliases like 'dir' and 'ls').\n\nTry it by typing: `Get-ChildItem`",
                "expectedCommand": "Get-ChildItem",
                "hint": "Type Get-ChildItem or use one of its aliases like 'dir' or 'ls'"
            }
        ]
    },
    {
        "id": "git-essentials",
        "title": "Git Essentials",
        "description": "Master the basics of Git version control with hands-on practice.",
        "difficulty": "Beginner",
        "steps": [
            {
                "id": 1,
                "instructions": "Initialize a new Git repository in the current directory using the git init command.",
                "expectedCommand": "git init",
                "hint": "Use the git init command to create a new repository"
            },
            {
                "id": 2,
                "instructions": "Check the status of your repository to see if there are any changes or untracked files.",
                "expectedCommand": "git status",
                "hint": "Use git status to see the current state of your repository"
            }
        ]
    },
    {
        "id": "python-basics",
        "title": "Python Basics",
        "description": "Introduction to Python programming with interactive examples.",
        "difficulty": "Beginner",
        "steps": [
            {
                "id": 1,
                "instructions": "Print 'Hello, Python!' to the console using the print() function.",
                "expectedCommand": "print('Hello, Python!')",
                "hint": "Use the print() function with your text in quotes"
            },
            {
                "id": 2,
                "instructions": "Create a variable 'name' with your name and then print a greeting using that variable.",
                "expectedCommand": "name = 'YourName'\nprint('Hello, ' + name + '!')",
                "hint": "First assign a string to the variable 'name', then use print() with concatenation"
            }
        ]
    }
]
