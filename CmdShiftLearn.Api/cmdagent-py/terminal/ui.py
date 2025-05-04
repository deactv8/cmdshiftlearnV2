"""
Terminal UI components for CmdShiftLearn.
"""

import os
import sys
from typing import Optional, List, Dict, Any, Callable

from prompt_toolkit import prompt
from prompt_toolkit.completion import WordCompleter
from prompt_toolkit.styles import Style
from prompt_toolkit.history import FileHistory
from prompt_toolkit.formatted_text import HTML
from rich.console import Console
from rich.panel import Panel
from rich.text import Text
from rich.markdown import Markdown
from rich.progress import Progress, SpinnerColumn, TextColumn
from rich.syntax import Syntax
from rich.table import Table

from utils.config import DATA_DIR

# Ensure the history directory exists
os.makedirs(os.path.join(DATA_DIR, 'history'), exist_ok=True)
HISTORY_FILE = os.path.join(DATA_DIR, 'history', 'command_history.txt')


class TerminalUI:
    """Terminal UI for CmdShiftLearn."""
    
    def __init__(self, app):
        """Initialize the terminal UI."""
        self.app = app
        self.console = Console()
        self.style = Style.from_dict({
            'welcome': '#00FFFF bold',
            'prompt': '#FFFFFF bold',
            'command': '#00FF00',
            'error': '#FF0000 bold',
            'success': '#00FF00 bold',
            'info': '#00FFFF',
            'warning': '#FFFF00',
            'hint': '#FFAA00',
        })
        
        # PowerShell command completer for auto-completion
        self.commands = [
            "Get-Command", "Get-Help", "Get-Process", "Get-Service",
            "Start-Process", "Stop-Process", "New-Item", "Remove-Item",
            "Set-Location", "Get-Location", "Get-Content", "Set-Content",
            "Get-ChildItem", "Select-Object", "Where-Object", "ForEach-Object"
        ]
        self.completer = WordCompleter(self.commands, ignore_case=True)
        
    def clear_screen(self):
        """Clear the terminal screen."""
        os.system('cls' if os.name == 'nt' else 'clear')
        
    def show_welcome_screen(self):
        """Display the welcome screen."""
        self.clear_screen()
        
        # Create logo text
        logo_text = Text()
        logo_text.append("\n")
        logo_text.append("  _____           _  _____ _     _  __ _   _                         \n", style="cyan")
        logo_text.append(" / ____|         | |/ ____| |   (_)/ _| | | |                        \n", style="cyan")
        logo_text.append("| |     _ __ ___ | | (___ | |__  _| |_| |_| |    ___  __ _ _ __ _ __ \n", style="blue")
        logo_text.append("| |    | '_ ` _ \\| |\\___ \\| '_ \\| |  _|  _  |   / _ \\/ _` | '__| '_ \\\n", style="blue")
        logo_text.append("| |____| | | | | | |____) | | | | | | | | | |  |  __/ (_| | |  | | | |\n", style="magenta")
        logo_text.append(" \\_____|_| |_| |_|_|_____/|_| |_|_|_| |_| |_|   \\___|\\__,_|_|  |_| |_|\n", style="magenta")
        logo_text.append("\n")
        
        # Create welcome panel
        welcome_panel = Panel(
            logo_text,
            title="PowerShell Learning Platform",
            subtitle="Master PowerShell & Prepare for Certification"
        )
        
        # Display welcome panel
        self.console.print(welcome_panel)
        
        # Display version information
        version = "v0.1.0"
        self.console.print(f"[cyan]CmdShiftLearn[/cyan] [yellow]{version}[/yellow]", justify="center")
        self.console.print("[italic]An interactive PowerShell learning platform[/italic]", justify="center")
        self.console.print()
        
    def prompt_for_username(self):
        """Prompt the user for their username."""
        self.console.print("[bold]Please enter your username to get started:[/bold]")
        return prompt("Username: ", style=self.style)
    
    def show_main_menu(self):
        """Display the main menu and return the user's selection."""
        self.console.print("\n[bold cyan]Main Menu[/bold cyan]")
        
        # Create menu table
        table = Table(show_header=False, box=None, padding=(0, 2, 0, 0))
        table.add_column("Option", style="cyan")
        table.add_column("Description")
        
        table.add_row("1", "Start Tutorial")
        table.add_row("2", "Practice Challenges")
        table.add_row("3", "Certification Progress")
        table.add_row("4", "PowerShell Playground")
        table.add_row("5", "View Profile")
        table.add_row("6", "Settings")
        table.add_row("0", "Exit")
        
        self.console.print(table)
        self.console.print()
        
        # Get user selection
        choice = prompt("Select an option: ", style=self.style)
        return choice
    
    def start_main_loop(self):
        """Start the main application loop."""
        while True:
            choice = self.show_main_menu()
            
            if choice == "1":
                self.app.start_tutorial()
            elif choice == "2":
                self.app.start_challenge()
            elif choice == "3":
                self.app.view_certification_progress()
            elif choice == "4":
                self.app.start_powershell_playground()
            elif choice == "5":
                self.app.view_profile()
            elif choice == "6":
                self.app.show_settings()
            elif choice == "0":
                self.say_goodbye()
                break
            else:
                self.console.print("[bold red]Invalid option. Please try again.[/bold red]")
    
    def display_tutorials(self, tutorials: List[Dict[str, Any]]):
        """Display available tutorials."""
        self.console.print("\n[bold cyan]Available Tutorials[/bold cyan]")
        
        if not tutorials:
            self.console.print("[yellow]No tutorials available.[/yellow]")
            return None
        
        # Create tutorials table
        table = Table(show_header=True)
        table.add_column("#", style="cyan", justify="right")
        table.add_column("Title", style="green")
        table.add_column("Difficulty", style="yellow")
        table.add_column("XP", style="magenta", justify="right")
        table.add_column("Status", style="blue")
        
        # Add tutorials to table
        for i, tutorial in enumerate(tutorials, 1):
            title = tutorial.get('title', 'Unknown')
            difficulty = tutorial.get('difficulty', 'beginner').capitalize()
            xp_reward = tutorial.get('xp_reward', 0)
            status = self.app.get_tutorial_status(tutorial.get('id', ''))
            
            table.add_row(
                str(i),
                title,
                difficulty,
                str(xp_reward),
                status
            )
        
        self.console.print(table)
        
        # Get user selection
        self.console.print()
        choice = prompt("Select a tutorial (number) or 'b' to go back: ", style=self.style)
        
        if choice.lower() == 'b':
            return None
        
        try:
            index = int(choice) - 1
            if 0 <= index < len(tutorials):
                return tutorials[index]
            else:
                self.console.print(f"[bold red]Invalid selection. Please enter a number between 1 and {len(tutorials)}.[/bold red]")
                return None
        except ValueError:
            self.console.print("[bold red]Invalid input. Please enter a number.[/bold red]")
            return None
    
    def display_tutorial(self, tutorial: Dict[str, Any]):
        """Display a tutorial's details."""
        if not tutorial:
            self.console.print("[bold red]Tutorial not found.[/bold red]")
            return
        
        # Clear screen
        self.clear_screen()
        
        # Get tutorial details
        title = tutorial.get('title', 'Unknown')
        description = tutorial.get('description', 'No description available.')
        difficulty = tutorial.get('difficulty', 'beginner').capitalize()
        xp_reward = tutorial.get('xp_reward', 0)
        cert_mappings = tutorial.get('certification_mappings', [])
        
        # Create tutorial panel
        content = Text()
        content.append(f"{description}\n\n")
        content.append(f"Difficulty: ", style="bold")
        content.append(f"{difficulty}\n", style="yellow")
        content.append(f"XP Reward: ", style="bold")
        content.append(f"{xp_reward}\n", style="magenta")
        
        if cert_mappings:
            content.append(f"Certification Alignment:\n", style="bold")
            for mapping in cert_mappings:
                cert_id = mapping.get('cert_id', '').upper()
                domain = mapping.get('domain', 'Unknown Domain')
                content.append(f"  • {cert_id}: {domain}\n", style="blue")
        
        panel = Panel(
            content,
            title=f"[bold]{title}[/bold]",
            expand=False
        )
        
        self.console.print(panel)
        
        # Ask if user wants to start
        self.console.print()
        self.console.print("[bold]Would you like to start this tutorial?[/bold]")
        choice = prompt("Enter (y)es or (n)o: ", style=self.style)
        
        return choice.lower() == 'y'
    
    def get_command_input(self, step: Dict[str, Any]):
        """Get PowerShell command input from the user."""
        # Display prompt for PowerShell command
        self.console.print("\n[bold cyan]Enter your PowerShell command:[/bold cyan]")
        
        # Create history file if it doesn't exist
        if not os.path.exists(HISTORY_FILE):
            with open(HISTORY_FILE, 'w') as f:
                pass
        
        # Get command input with history and auto-completion
        user_input = prompt(
            "PS> ",
            history=FileHistory(HISTORY_FILE),
            completer=self.completer,
            style=self.style
        )
        
        return user_input
    
    def display_command_result(self, success: bool, stdout: str, stderr: str):
        """Display the result of a PowerShell command execution."""
        if success:
            if stdout.strip():
                self.console.print(Panel(stdout, title="Output", border_style="green"))
        else:
            if stderr.strip():
                self.console.print(Panel(stderr, title="Error", border_style="red"))
    
    def display_feedback(self, is_correct: bool, feedback: str, hint: str = None):
        """Display feedback about the user's command."""
        if is_correct:
            self.console.print(f"\n[bold green]{feedback}[/bold green]")
        else:
            self.console.print(f"\n[bold red]{feedback}[/bold red]")
            if hint:
                self.console.print(f"[bold yellow]Hint:[/bold yellow] {hint}")
    
    def display_tutorial_step(self, step: Dict[str, Any], step_number: int, total_steps: int):
        """Display a tutorial step."""
        # Get step details
        title = step.get('title', f'Step {step_number}')
        content = step.get('content', 'No content available.')
        
        # Create progress bar
        progress_text = f"Step {step_number}/{total_steps}"
        progress = (step_number / total_steps) * 100
        
        # Display step header
        self.console.print()
        self.console.print(f"[bold cyan]{progress_text}[/bold cyan] [yellow]{progress:.0f}%[/yellow]")
        self.console.print(f"[bold green]{title}[/bold green]")
        
        # Display step content - support for markdown
        if "```" in content or "#" in content or "*" in content:
            self.console.print(Markdown(content))
        else:
            self.console.print(content)
    
    def display_challenge(self, challenge: Dict[str, Any]):
        """Display a challenge to the user."""
        # Challenge display implementation
        title = challenge.get('title', 'Challenge')
        description = challenge.get('description', 'No description available.')
        difficulty = challenge.get('difficulty', 'beginner').capitalize()
        xp_reward = challenge.get('xp_reward', 0)
        
        # Create challenge panel
        content = Text()
        content.append(f"{description}\n\n")
        content.append(f"Difficulty: ", style="bold")
        content.append(f"{difficulty}\n", style="yellow")
        content.append(f"XP Reward: ", style="bold")
        content.append(f"{xp_reward}", style="magenta")
        
        panel = Panel(
            content,
            title=f"[bold]{title}[/bold]",
            expand=False
        )
        
        self.console.print(panel)
    
    def display_achievement(self, achievement: Dict[str, Any]):
        """Display an achievement notification."""
        title = achievement.get('title', 'Achievement Unlocked!')
        description = achievement.get('description', '')
        xp_reward = achievement.get('xp_reward', 0)
        
        # Create achievement panel
        content = Text()
        content.append(f"{description}\n")
        if xp_reward > 0:
            content.append(f"+{xp_reward} XP", style="bold magenta")
        
        panel = Panel(
            content,
            title=f"[bold yellow]{title}[/bold yellow]",
            border_style="yellow",
            expand=False
        )
        
        # Play achievement sound (if available)
        # self._play_achievement_sound()
        
        # Display achievement notification
        self.console.print()
        self.console.print(panel)
        self.console.print()
        
        # Wait for user acknowledgment
        prompt("Press Enter to continue...", style=self.style)
    
    def display_certification_progress(self, certifications: List[Dict[str, Any]]):
        """Display certification progress."""
        self.console.print("\n[bold cyan]Certification Progress[/bold cyan]")
        
        if not certifications:
            self.console.print("[yellow]No certification progress available.[/yellow]")
            return
        
        # Display each certification
        for cert in certifications:
            cert_id = cert.get('id', '').upper()
            title = cert.get('title', f'Certification {cert_id}')
            progress = cert.get('progress', 0)
            domains = cert.get('domains', [])
            
            # Create certification panel
            content = Text()
            content.append(f"Overall Progress: ", style="bold")
            content.append(f"{progress:.1f}%\n\n", style="green" if progress >= 75 else "yellow" if progress >= 50 else "red")
            
            # Add domain progress
            if domains:
                content.append("Domains:\n", style="bold")
                for domain in domains:
                    domain_name = domain.get('name', 'Unknown')
                    domain_progress = domain.get('progress', 0)
                    content.append(f"  • {domain_name}: ", style="bold")
                    content.append(f"{domain_progress:.1f}%\n", 
                                style="green" if domain_progress >= 75 else "yellow" if domain_progress >= 50 else "red")
            
            panel = Panel(
                content,
                title=f"[bold]{title}[/bold]",
                expand=False
            )
            
            self.console.print(panel)
            self.console.print()
    
    def display_profile(self, profile: Dict[str, Any]):
        """Display user profile information."""
        username = profile.get('username', 'Unknown')
        level = profile.get('level', 1)
        xp = profile.get('xp', 0)
        next_level_xp = profile.get('next_level_xp', 100)
        achievements = profile.get('achievements', [])
        completed_tutorials = profile.get('completed_tutorials', [])
        
        # Calculate XP progress percentage
        progress_percentage = (xp / next_level_xp) * 100 if next_level_xp > 0 else 100
        
        # Create profile panel
        content = Text()
        content.append(f"Level: ", style="bold")
        content.append(f"{level}\n", style="cyan")
        content.append(f"XP: ", style="bold")
        content.append(f"{xp}/{next_level_xp} ", style="magenta")
        content.append(f"({progress_percentage:.1f}%)\n\n", style="yellow")
        
        # Add completed tutorials
        content.append(f"Completed Tutorials: {len(completed_tutorials)}\n", style="bold")
        
        # Add achievements
        content.append(f"Achievements: {len(achievements)}\n", style="bold")
        if achievements:
            for achievement in achievements[:3]:  # Show only the first 3
                content.append(f"  • {achievement.get('title', 'Unknown')}\n", style="green")
            if len(achievements) > 3:
                content.append(f"  ... and {len(achievements) - 3} more\n", style="dim")
        
        panel = Panel(
            content,
            title=f"[bold]{username}'s Profile[/bold]",
            expand=False
        )
        
        self.console.print(panel)
        
        # Wait for user acknowledgment
        self.console.print()
        prompt("Press Enter to continue...", style=self.style)
    
    def say_goodbye(self):
        """Display goodbye message."""
        self.console.print(Panel("[bold cyan]Thank you for using CmdShiftLearn![/bold cyan]\nSee you next time!"))
