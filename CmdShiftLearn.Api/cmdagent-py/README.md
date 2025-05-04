# CmdShiftLearn

An interactive PowerShell learning platform with certification alignment and gamification.

## Overview

CmdShiftLearn is a terminal-based application that helps you learn PowerShell through interactive tutorials, challenges, and certification-aligned content. The platform includes:

- Interactive PowerShell tutorials
- Practice challenges
- Certification progress tracking (AZ-104, SC-300)
- XP system and achievements
- PowerShell playground for experimentation

## Features

- **Terminal-based UI**: User-friendly interface with rich formatting
- **Interactive Tutorials**: Step-by-step tutorials with command validation
- **Challenges**: Test your skills with practical challenges
- **Certification Alignment**: Track your progress towards Microsoft certifications
- **Gamification**: Earn XP, level up, and unlock achievements
- **PowerShell Integration**: Execute real PowerShell commands in a safe environment
- **Progress Tracking**: Resume tutorials and track your learning journey

## Installation

1. Make sure you have Python 3.7+ installed
2. Clone this repository
3. Install dependencies:

```bash
pip install -r requirements.txt
```

## Usage

Run the application:

```bash
python run.py
```

Or directly:

```bash
python app.py
```

## Getting Started

1. Enter your username when prompted
2. Select "Start Tutorial" from the main menu
3. Choose a tutorial from the list
4. Follow the instructions and complete the steps
5. Earn XP and achievements as you progress

## Available Tutorials

- **PowerShell Basics**: Learn fundamental PowerShell commands
- More tutorials coming soon!

## Challenges

Practice your skills with challenges:

- **Filter Processes Challenge**: Practice filtering and sorting processes
- More challenges coming soon!

## Certifications

Track your progress towards these Microsoft certifications:

- **AZ-104**: Microsoft Azure Administrator
- **SC-300**: Microsoft Identity and Access Administrator

## Development

### Project Structure

```
cmdshiftlearn/
├── app.py                  # Main application entry point
├── terminal/               # Terminal UI components
├── powershell/             # PowerShell integration
├── content/                # Content management
├── user/                   # User profile and progress
├── gamification/           # XP and achievements
├── certification/          # Certification tracking
├── data/                   # Data storage
│   ├── content/            # Tutorial content files
│   ├── users/              # User data
│   └── config.json         # Configuration
└── utils/                  # Utility functions
```

### Adding New Content

Tutorials and challenges are defined in YAML files in the `data/content/` directory:

- Tutorials: `data/content/tutorials/`
- Challenges: `data/content/challenges/`
- Certifications: `data/content/certifications/`

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Roadmap

- Add more tutorials and challenges
- Implement full certification practice exams
- Add Azure lab integration
- Develop web dashboard for certification tracking
- Create community features
