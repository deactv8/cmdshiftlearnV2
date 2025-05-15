"""
Enhanced Terminal UI with animations for CmdShiftLearn.
"""

import os
import sys
import time
import random
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
from rich.progress import Progress, SpinnerColumn, TextColumn, BarColumn, TaskProgressColumn
from rich.syntax import Syntax
from rich.table import Table
from rich.align import Align
from rich.live import Live
from rich.rule import Rule
from rich.box import ROUNDED, DOUBLE, HEAVY

from utils.config import DATA_DIR

# Ensure the history directory exists
os.makedirs(os.path.join(DATA_DIR, 'history'), exist_ok=True)
HISTORY_FILE = os.path.join(DATA_DIR, 'history', 'command_history.txt')


class AnimatedTerminalUI:
    """Enhanced Terminal UI with animations for CmdShiftLearn."""
    
    def __init__(self, app=None):
        """Initialize the animated terminal UI."""
        self.app = app
        try:
            # Try to create the console with support for Unicode characters
            self.console = Console(legacy_windows=False)
            
            # Test if we can actually print Unicode characters
            test_char = "—"  # em dash, commonly problematic
            self.console.print(test_char, end="")
            self.console.print()  # Clear the line
            unicode_supported = True
        except:
            # Fallback to legacy Windows mode if Unicode fails
            self.console = Console(legacy_windows=True)
            unicode_supported = False
            
        # Store whether we have Unicode support
        self.unicode_supported = unicode_supported
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
        
        # Typing animation speed (characters per second)
        self.typing_speed = 0.02
        
        # Celebration emojis for feedback
        self.celebration_emojis = ["🎉", "🚀", "⭐", "🏆", "🎮", "🎯", "💯", "👍"]
        
        # Track total XP for animations
        self.current_total_xp = 0
        
        # PowerShell command completer for auto-completion
        self.commands = [
            "Get-Command", "Get-Help", "Get-Process", "Get-Service",
            "Start-Process", "Stop-Process", "New-Item", "Remove-Item",
            "Set-Location", "Get-Location", "Get-Content", "Set-Content",
            "Get-ChildItem", "Select-Object", "Where-Object", "ForEach-Object"
        ]
        self.completer = WordCompleter(self.commands, ignore_case=True)
    
    def set_typing_speed(self, speed: float):
        """
        Set the typing animation speed.
        
        Args:
            speed: Delay in seconds between characters
        """
        self.typing_speed = speed
    
    def clear_screen(self):
        """Clear the terminal screen."""
        self.console.clear()
    
    def animated_text(self, text: str, delay: float = None) -> None:
        """
        Print text with a typing animation effect.
        
        Args:
            text: Text to animate
            delay: Override the default typing speed
        """
        delay = delay or self.typing_speed
        
        for char in text:
            print(char, end='', flush=True)
            time.sleep(delay)
        print()
    
    def animated_rich_text(self, text: str, style: str = None, delay: float = None) -> None:
        """
        Print styled text with a typing animation effect.
        
        Args:
            text: Text to animate
            style: Rich text style
            delay: Override the default typing speed
        """
        delay = delay or self.typing_speed
        
        # For styled text, use a live display
        if style:
            # Apply the style directly to the text object
            styled_text = Text(text, style=style)
            self.console.print(styled_text)
            return
            
        # For texts with markup (like [bold green]), print directly
        self.console.print(text)
    
    def animated_markdown(self, markdown_text: str, delay: float = None) -> None:
        """
        Print markdown with a typing animation effect.
        
        Args:
            markdown_text: Markdown text to animate
            delay: Override the default typing speed
        """
        delay = delay or self.typing_speed
        
        # Create a live display that will update with each character
        md = Markdown("")
        with Live(md, console=self.console, refresh_per_second=20) as live:
            displayed_text = ""
            for char in markdown_text:
                displayed_text += char
                md = Markdown(displayed_text)
                live.update(md)
                time.sleep(delay)
    
    def show_welcome_screen(self):
        """Display the welcome screen with animations."""
        self.clear_screen()
        
        # Create logo text
        logo_text = """
  _____           _  _____ _     _  __ _   _                         
 / ____|         | |/ ____| |   (_)/ _| | | |                        
| |     _ __ ___ | | (___ | |__  _| |_| |_| |    ___  __ _ _ __ _ __ 
| |    | '_ ` _ \\| |\\___ \\| '_ \\| |  _|  _  |   / _ \\/ _` | '__| '_ \\
| |____| | | | | | |____) | | | | | | | | | |  |  __/ (_| | |  | | | |
 \\_____|_| |_| |_|_|_____/|_| |_|_|_| |_| |_|   \\___|\\__,_|_|  |_| |_|
"""
        
        # Animate the logo appearance line by line
        lines = logo_text.split('\n')
        for line in lines:
            if line.strip():  # Skip empty lines
                self.console.print(line, style="cyan")
                time.sleep(0.1)
        
        # Animate the subtitle appearance
        subtitle = "PowerShell Learning Platform"
        self.animated_rich_text(subtitle, style="bold blue", delay=0.05)
        
        # Animate the version information with a "typing" effect
        time.sleep(0.5)
        version = "v1.0.0"
        version_text = f"CmdShiftLearn {version}"
        self.animated_rich_text(version_text, style="yellow", delay=0.03)
        
        # Animate the description
        time.sleep(0.3)
        description = "An interactive PowerShell learning platform"
        self.animated_rich_text(description, style="italic", delay=0.03)
        
        # Pause for effect
        time.sleep(1)
    
    def prompt_for_username(self):
        """Prompt the user for their username with animation."""
        # Animate the prompt appearance
        prompt_text = "Please enter your username to get started:"
        self.animated_rich_text(prompt_text, style="bold", delay=0.02)
        
        # Create a blinking cursor effect
        with Live(console=self.console, refresh_per_second=4) as live:
            for i in range(3):
                cursors = ["_", " "]
                for cursor in cursors:
                    live.update(Text(f"Username: {cursor}", style="cyan bold"))
                    time.sleep(0.25)
        
        # Get the actual input
        return prompt("Username: ", style=self.style)
    
    def show_animated_menu(self, title: str, options: List[Dict[str, str]]):
        """
        Display an animated menu and return the user's selection.
        
        Args:
            title: Menu title
            options: List of option dictionaries with 'key' and 'description'
            
        Returns:
            str: Selected option key
        """
        # Animate the menu title
        self.console.print()
        title_text = f"[bold cyan]{title}[/bold cyan]"
        self.animated_rich_text(title_text, delay=0.01)
        
        # Create menu table with animation
        table = Table(show_header=False, box=None, padding=(0, 2, 0, 0))
        table.add_column("Option", style="cyan")
        table.add_column("Description")
        
        # Add options with animation
        with Live(table, console=self.console, refresh_per_second=4) as live:
            for option in options:
                table.add_row(option['key'], option['description'])
                live.update(table)
                time.sleep(0.1)
        
        self.console.print()
        
        # Animate the prompt
        prompt_text = "[bold yellow]Select an option:[/bold yellow]"
        self.animated_rich_text(prompt_text, delay=0.01)
        
        # Get user selection
        choice = prompt("> ", style=self.style)
        return choice
    
    def show_main_menu(self):
        """Display the main menu with animations and return the user's selection."""
        options = [
            {'key': '1', 'description': 'Start Tutorial'},
            {'key': '2', 'description': 'Practice Challenges'},
            {'key': '3', 'description': 'Certification Progress'},
            {'key': '4', 'description': 'PowerShell Playground'},
            {'key': '5', 'description': 'View Profile'},
            {'key': '6', 'description': 'Settings'},
            {'key': '0', 'description': 'Exit'}
        ]
        
        return self.show_animated_menu("Main Menu", options)
    
    def display_tutorials_animated(self, tutorials: List[Dict[str, Any]]):
        """Display available tutorials with animation effects."""
        self.console.print()
        
        # Animate header
        header_text = "[bold cyan]Available Tutorials[/bold cyan]"
        self.animated_rich_text(header_text, delay=0.01)
        
        if not tutorials:
            no_tutorials = "[yellow]No tutorials available.[/yellow]"
            self.animated_rich_text(no_tutorials, delay=0.01)
            return None
        
        # Create tutorials table
        table = Table(show_header=True)
        table.add_column("#", style="cyan", justify="right")
        table.add_column("Title", style="green")
        table.add_column("Difficulty", style="yellow")
        table.add_column("XP", style="magenta", justify="right")
        
        # Add tutorials to table with animation
        with Live(table, console=self.console, refresh_per_second=4) as live:
            for i, tutorial in enumerate(tutorials, 1):
                title = tutorial.get('title', 'Unknown')
                difficulty = tutorial.get('difficulty', 'beginner').capitalize()
                xp_reward = tutorial.get('xpTotal', 0)
                
                table.add_row(
                    str(i),
                    title,
                    difficulty,
                    str(xp_reward)
                )
                live.update(table)
                time.sleep(0.2)
        
        # Animate the prompt
        self.console.print()
        prompt_text = "[bold yellow]Select a tutorial (number) or 'b' to go back:[/bold yellow]"
        self.animated_rich_text(prompt_text, delay=0.01)
        
        # Get user selection
        choice = prompt("> ", style=self.style)
        
        if choice.lower() == 'b':
            return None
        
        try:
            index = int(choice) - 1
            if 0 <= index < len(tutorials):
                return tutorials[index]
            else:
                error_text = f"[bold red]Invalid selection. Please enter a number between 1 and {len(tutorials)}.[/bold red]"
                self.animated_rich_text(error_text, delay=0.01)
                return None
        except ValueError:
            error_text = "[bold red]Invalid input. Please enter a number.[/bold red]"
            self.animated_rich_text(error_text, delay=0.01)
            return None
    
    def display_tutorial_header(self, tutorial: Dict[str, Any]) -> None:
        """
        Display tutorial header with metadata using animated styling.
        
        Args:
            tutorial: Tutorial data
        """
        if not tutorial:
            self.console.print("[bold red]Tutorial not found or could not be loaded.[/bold red]")
            return
        
        title = tutorial.get('title', 'Untitled Tutorial')
        description = tutorial.get('description', 'No description available.')
        difficulty = tutorial.get('difficulty', 'Unknown')
        
        # Handle both old and new XP field names
        xp_total = tutorial.get('xpTotal', tutorial.get('xp', 0))
        
        # Create and animate a header panel
        self.clear_screen()
        
        # Add stylish rule (safely)
        try:
            self.console.rule("[bold cyan]CmdShiftLearn Tutorial[/bold cyan]")
        except Exception:
            # Fallback if rule rendering fails due to encoding issues
            self.console.print("=" * 60)
            self.console.print("[bold cyan]CmdShiftLearn Tutorial[/bold cyan]", justify="center")
            self.console.print("=" * 60)
        
        # Animate title with typing effect
        self.console.print()
        title_text = f"[bold blue]{title}[/bold blue]"
        self.animated_rich_text(title_text, delay=0.01)
        
        # Display difficulty and XP with a subtle fade-in effect
        info_text = f"[bold]Difficulty:[/bold] [yellow]{difficulty}[/yellow] | [bold]XP:[/bold] [magenta]{xp_total}[/magenta]"
        self.animated_rich_text(info_text, delay=0.01)
        
        # Animate description in a panel
        self.console.print()
        
        try:
            from rich.panel import Panel
            from rich.markdown import Markdown
            from rich.box import ROUNDED
            
            # Use markdown for the description
            md = Markdown(description)
            description_panel = Panel(
                md,
                title="[bold green]Description[/bold green]",
                border_style="cyan",
                box=ROUNDED,
                padding=(1, 2)
            )
            self.console.print(description_panel)
        except Exception:
            # Fallback for simple text version
            self.console.print("[bold]Description:[/bold]")
            self.animated_rich_text(description, delay=0.01)
        
        self.console.print()
        
        # Add a visual separator (safely)
        try:
            self.console.rule("[bold cyan]Let's Begin![/bold cyan]")
        except Exception:
            # Fallback for encoding issues
            self.console.print("=" * 60)
            self.console.print("[bold cyan]Let's Begin![/bold cyan]", justify="center")
            self.console.print("=" * 60)
        
        time.sleep(0.5)  # Pause for effect
    
    def display_step(self, step: Dict[str, Any], step_number: int, total_steps: int) -> None:
        """
        Display a tutorial step with animations and visual styling.
        
        Args:
            step: Step data
            step_number: Current step number (1-based)
            total_steps: Total number of steps
        """
        if not step:
            self.console.print("[yellow]This step is empty or not available.[/yellow]")
            return
        
        # Extract step details
        step_id = step.get('id', f'step{step_number}')
        title = step.get('title', f'Step {step_number}')
        instruction = step.get('instructions', 'No instructions available.')
        xp_reward = step.get('xpReward', step.get('xp', 0))
        
        # Create progress text
        progress = f"Step {step_number} of {total_steps}"
        
        # Clear the screen and display step header
        self.clear_screen()
        
        # Display step progress with a progress bar
        self.console.print()
        progress_pct = (step_number - 1) / total_steps  # -1 because we're at the start of this step
        
        try:
            with Progress(
                "[progress.description]{task.description}",
                BarColumn(complete_style="green", finished_style="green"),
                "[progress.percentage]{task.percentage:>3.0f}%",
                console=self.console
            ) as progress_bar:
                task = progress_bar.add_task(f"[cyan]Tutorial Progress[/cyan]", total=100, completed=progress_pct * 100)
                time.sleep(0.5)  # Let user see current progress
        except Exception:
            # Fallback for encoding issues
            self.console.print(f"[cyan]Tutorial Progress: {int(progress_pct * 100)}%[/cyan]")
            self.console.print("=" * int(60 * progress_pct) + ">" + " " * int(60 * (1 - progress_pct)))
            time.sleep(0.5)
        
        # Display step title with animation
        self.console.print()
        step_title = f"[bold blue]{title}[/bold blue]"
        self.animated_rich_text(step_title, delay=0.01)
        
        # Display instructions in a more prominent way with a box or panel
        self.console.print()
        
        try:
            # Create a panel for instructions with pretty formatting
            from rich.panel import Panel
            from rich.markdown import Markdown
            from rich.box import ROUNDED
            
            # Format the instructions as markdown
            if "```" in instruction or "#" in instruction or "*" in instruction or "🔍" in instruction or "👋" in instruction:
                md = Markdown(instruction)
                # Use a panel with a nice border to make instructions stand out
                instruction_panel = Panel(
                    md,
                    title="[bold green]Instructions[/bold green]",
                    border_style="green",
                    box=ROUNDED,
                    padding=(1, 2)
                )
                self.console.print(instruction_panel)
            else:
                # Plain text format if no markdown
                self.console.print("[bold green]Instructions:[/bold green]")
                self.console.print(instruction, style="white")
        except Exception:
            # Fallback if rich formatting fails
            self.console.print("[bold green]Instructions:[/bold green]")
            self.console.print(instruction)
        
        # Display XP reward info with subtle animation
        if xp_reward:
            self.console.print()
            xp_text = f"[green]🏆 Complete this step to earn {xp_reward} XP![/green]"
            
            try:
                self.console.print(xp_text)
            except Exception:
                # Fallback if animation fails
                self.console.print(f"[green]Complete this step to earn {xp_reward} XP![/green]")
        
        self.console.print()
        
        # Make a clear visual separator before the command prompt
        try:
            self.console.rule("[bold yellow]Your Turn![/bold yellow]")
        except Exception:
            # Fallback for encoding issues
            self.console.print("\n" + "-" * 60)
            self.console.print("[bold yellow]Your Turn![/bold yellow]")
            self.console.print("-" * 60)
            
        self.console.print()
    
    def get_command_input(self, step: Dict[str, Any]):
        """Get PowerShell command input from the user with animation."""
        # Animate the prompt appearance
        self.console.print()
        prompt_text = "[bold cyan]Enter your PowerShell command:[/bold cyan]"
        self.animated_rich_text(prompt_text, delay=0.01)
        
        # Create a more visually distinct prompt
        try:
            # Use a blue background for the PS> prompt to make it stand out
            from rich.console import Console
            from rich.text import Text
            
            # Show the prompt with background formatting
            prompt_style = "white on blue"
            ps_prompt = Text("PS>", style=prompt_style)
            self.console.print(ps_prompt, end=" ")
        except Exception:
            # Fallback if rich formatting fails
            print("PS>", end=" ")
        
        # Create history file if it doesn't exist
        if not os.path.exists(HISTORY_FILE):
            with open(HISTORY_FILE, 'w') as f:
                pass
        
        # Get command input with history and auto-completion
        try:
            user_input = prompt(
                "",  # Use an empty prompt since we've already displayed PS>
                history=FileHistory(HISTORY_FILE),
                completer=self.completer,
                style=self.style
            )
        except Exception:
            # Fallback if prompt_toolkit fails
            user_input = input()
        
        return user_input
    
    def display_feedback(self, is_correct: bool, feedback: str) -> None:
        """
        Display animated feedback for a command.
        
        Args:
            is_correct: Whether the command was correct
            feedback: Feedback message
        """
        self.console.print()
        
        try:
            # Create a visually appealing panel for feedback
            from rich.panel import Panel
            from rich.box import ROUNDED
            
            if is_correct:
                # Success feedback - in a green panel
                panel = Panel(
                    feedback,
                    title="[bold green]Success![/bold green]",
                    border_style="green",
                    box=ROUNDED
                )
                self.console.print(panel)
            else:
                # Error feedback - in a red panel
                panel = Panel(
                    feedback,
                    title="[bold red]Try Again[/bold red]",
                    border_style="red",
                    box=ROUNDED
                )
                self.console.print(panel)
        except Exception:
            # Fallback if panel formatting fails
            if is_correct:
                # Success feedback - directly use console.print
                self.console.print(f"[bold green]✓ {feedback}[/bold green]")
            else:
                # Error feedback
                self.console.print(f"[bold red]✗ {feedback}[/bold red]")
                
        # Pause slightly to let the user read the feedback
        time.sleep(0.5)
    
    def display_hint(self, hint: str) -> None:
        """
        Display a hint with animated reveal.
        
        Args:
            hint: Hint text
        """
        self.console.print()
        
        # Animate a "typing" hint panel
        hint_panel = Panel(
            Markdown(""),
            title="[bold yellow]Hint[/bold yellow]",
            border_style="yellow",
            box=ROUNDED
        )
        
        with Live(hint_panel, console=self.console, refresh_per_second=20) as live:
            displayed_text = ""
            for char in hint:
                displayed_text += char
                hint_panel = Panel(
                    Markdown(displayed_text),
                    title="[bold yellow]Hint[/bold yellow]",
                    border_style="yellow",
                    box=ROUNDED
                )
                live.update(hint_panel)
                time.sleep(self.typing_speed)
        
        self.console.print()
    
    def display_expected_command(self, command: str) -> None:
        """
        Display the expected command with animated reveal.
        
        Args:
            command: Expected command
        """
        self.console.print()
        self.console.print("[bold yellow]Expected Command:[/bold yellow]")
        
        # Create a syntax object for the command
        syntax = Syntax("", "powershell", theme="monokai", line_numbers=False)
        
        # Animate it character by character
        with Live(syntax, console=self.console, refresh_per_second=20) as live:
            for i in range(len(command) + 1):
                partial_command = command[:i]
                syntax = Syntax(partial_command, "powershell", theme="monokai", line_numbers=False)
                live.update(syntax)
                time.sleep(self.typing_speed)
    
    def animate_xp_gain(self, xp: int) -> None:
        """
        Animate XP gain with visual and progress effects.
        
        Args:
            xp: XP points gained
        """
        self.console.print()
        
        # Track total XP
        old_xp = self.current_total_xp
        self.current_total_xp += xp
        
        # Create a progress bar for XP animation
        with Progress(
            SpinnerColumn(),
            TextColumn("[bold blue]Gaining XP..."),
            BarColumn(complete_style="green", finished_style="green"),
            TextColumn("[bold]{task.percentage:.0f}%"),
            console=self.console
        ) as progress:
            task = progress.add_task("", total=100)
            
            # Animate progress
            for i in range(1, 101):
                progress.update(task, completed=i)
                time.sleep(0.01)
        
        # Show XP gained with counting animation
        with Live(console=self.console, refresh_per_second=20) as live:
            for i in range(11):
                percentage = i / 10
                current_displayed_xp = int(old_xp + (xp * percentage))
                # Get a random emoji from our celebration set
                emoji = random.choice(self.celebration_emojis)
                live.update(Text(
                    f"{emoji} +{int(xp * percentage)} XP gained! Total: {current_displayed_xp} XP",
                    style="green bold"
                ))
                time.sleep(0.05)
        
        self.console.print()
    
    def celebrate_step_completion(self) -> None:
        """Display celebration for completing a step with animations."""
        celebrations = [
            "🎉 Great job!",
            "🚀 You're making progress!",
            "⭐ Excellent work!",
            "🏆 Well done!",
            "🎮 Level up!",
            "🎯 You nailed it!",
            "💯 Perfect!",
            "👍 Keep it up!"
        ]
        
        celebration = random.choice(celebrations)
        
        self.console.print()
        
        # Animate the celebration with growing/shrinking effect
        with Live(console=self.console, refresh_per_second=10) as live:
            for i in range(10):
                # Change the color to create a pulsing effect
                colors = ["green", "green1", "green2", "green3", "green4"]
                color = colors[i % len(colors)]
                live.update(Text(celebration, style=f"bold {color}"))
                time.sleep(0.1)
        
        self.console.print()
    
    def celebrate_tutorial_completion(self, tutorial_title: str, total_xp: int) -> None:
        """
        Display celebration for completing a tutorial with fireworks animation.
        
        Args:
            tutorial_title: Title of the completed tutorial
            total_xp: Total XP gained from the tutorial
        """
        self.console.print()
        
        # ASCII art for fireworks
        fireworks_frames = [
            """
             *  *
           *     * 
         *        *
           *     *
             *  *
            """,
            """
              \\ /
           - -X- -
              / \\
            """,
            """
           * * * *
          *       *
         *         *
          *       *
           * * * *
            """
        ]
        
        # Create celebration text
        celebration_text = f"""
        # 🎉 Congratulations! 🎉
        
        You have completed:
        ## {tutorial_title}
        
        Total XP earned: {total_xp} XP
        
        Keep going to master PowerShell!
        """
        
        # Animate fireworks
        for _ in range(3):  # Three cycles of fireworks
            for frame in fireworks_frames:
                self.clear_screen()
                self.console.print(frame, style="bold yellow")
                self.console.print(Markdown(celebration_text))
                time.sleep(0.2)
        
        # Final static display
        self.clear_screen()
        panel = Panel(
            Align.center(
                Markdown(celebration_text)
            ),
            title="[bold green]Tutorial Completed![/bold green]",
            border_style="green",
            box=HEAVY
        )
        self.console.print(panel)
        
        # Add some pause for effect
        time.sleep(1)
    
    def display_loading(self, message: str, callback: Callable) -> Any:
        """
        Display a loading spinner while executing a callback.
        
        Args:
            message: Loading message
            callback: Function to execute while showing spinner
            
        Returns:
            Any: Result of the callback
        """
        with self.console.status(f"[bold blue]{message}[/bold blue]", spinner="dots"):
            result = callback()
        
        return result
    
    def display_error(self, message: str) -> None:
        """
        Display an error message with visual effects.
        
        Args:
            message: Error message
        """
        self.console.print()
        
        # Animate the error appearance
        with Live(console=self.console, refresh_per_second=10) as live:
            for i in range(5):
                if i % 2 == 0:
                    live.update(Text(f"Error: {message}", style="bold red"))
                else:
                    live.update(Text(f"Error: {message}", style="red"))
                time.sleep(0.2)
        
        self.console.print()
    
    def say_goodbye(self):
        """Display animated goodbye message."""
        # Create goodbye message
        goodbye_text = "Thank you for using CmdShiftLearn!\nSee you next time!"
        
        # Animate the message character by character
        panel = Panel("")
        with Live(panel, console=self.console, refresh_per_second=20) as live:
            displayed_text = ""
            for char in goodbye_text:
                displayed_text += char
                panel = Panel(
                    Text(displayed_text, style="cyan bold"),
                    border_style="blue"
                )
                live.update(panel)
                time.sleep(0.05)
        
        # Fade out animation
        time.sleep(1)
