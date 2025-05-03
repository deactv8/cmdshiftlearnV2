#!/usr/bin/env python3
"""
CmdShiftLearn CLI Agent - Python Edition

A command-line tool for interacting with the CmdShiftLearn platform.
"""

import sys
import logging
from typing import List, Dict, Any, Optional, Tuple

# Get the logger
logger = logging.getLogger('main')

try:
    from rich.console import Console
    from rich.table import Table
    from rich.panel import Panel
    from rich.markdown import Markdown
    from rich.prompt import Prompt, Confirm
    from rich.syntax import Syntax
    RICH_AVAILABLE = True
except ImportError:
    RICH_AVAILABLE = False

from api.tutorials import TutorialClient
from api.auth import login, load_api_key


# Initialize Rich console if available
console = Console() if RICH_AVAILABLE else None


def print_header(text: str) -> None:
    """Print a formatted header."""
    if RICH_AVAILABLE:
        console.print(f"\n[bold blue]{text}[/bold blue]", justify="center")
    else:
        print("\n" + "=" * 60)
        print(f"{text:^60}")
        print("=" * 60 + "\n")


def print_tutorials(tutorials: List[Dict[str, Any]]) -> None:
    """Print tutorials in a formatted table."""
    if not tutorials:
        if RICH_AVAILABLE:
            console.print("[yellow]No tutorials found.[/yellow]")
        else:
            print("No tutorials found.")
        return
    
    if RICH_AVAILABLE:
        table = Table(title="Available Tutorials")
        table.add_column("#", style="cyan", justify="right")
        table.add_column("ID", style="green")
        table.add_column("Title", style="magenta")
        table.add_column("Difficulty", style="yellow")
        
        # Add tutorials to the table
        for i, tutorial in enumerate(tutorials, 1):
            table.add_row(
                str(i), 
                tutorial.get('id', 'N/A'), 
                tutorial.get('title', 'Untitled'),
                tutorial.get('difficulty', 'N/A')
            )
        
        # Display the table
        console.print(table)
    else:
        # Calculate column widths
        id_width = max(len("ID"), max(len(str(t.get('id', 'N/A'))) for t in tutorials))
        title_width = max(len("TITLE"), max(len(str(t.get('title', 'Untitled'))) for t in tutorials))
        
        # Print table header
        print(f"{'#':<4} | {' ID':<{id_width}} | {'TITLE':<{title_width}}")
        print("-" * (7 + id_width + title_width))
        
        # Print table rows
        for i, tutorial in enumerate(tutorials, 1):
            print(f"{i:<4} | {tutorial.get('id', 'N/A'):<{id_width}} | {tutorial.get('title', 'Untitled'):<{title_width}}")


def display_tutorial_header(tutorial: Dict[str, Any]) -> None:
    """Display a tutorial's header and description."""
    if not tutorial:
        if RICH_AVAILABLE:
            console.print("[bold red]Tutorial not found or could not be loaded.[/bold red]")
        else:
            print("Tutorial not found or could not be loaded.")
        return
    
    title = tutorial.get('title', 'Untitled Tutorial')
    description = tutorial.get('description', 'No description available.')
    
    if RICH_AVAILABLE:
        # Display tutorial header
        console.print(Panel(f"[bold blue]{title}[/bold blue]", expand=False))
        
        # Display description
        console.print("[bold]Description:[/bold]")
        console.print(f"[italic]{description}[/italic]")
        console.print()
    else:
        # Plain text fallback
        print(f"\n{title}\n{'=' * len(title)}")
        print(f"\nDescription: {description}\n")


def display_step(step: Dict[str, Any], step_number: int, show_expected: bool = False) -> None:
    """Display a tutorial step's instructions and optionally the expected command."""
    if not step:
        if RICH_AVAILABLE:
            console.print("[yellow]This step is empty or not available.[/yellow]")
        else:
            print("This step is empty or not available.")
        return
    
    instructions = step.get('instructions', 'No instructions available.')
    expected_command = step.get('expectedCommand', 'No command specified.')
    
    if RICH_AVAILABLE:
        # Display step header
        console.print(f"[bold green]Step {step_number}:[/bold green]")
        
        # Render instructions as markdown if they contain markdown formatting
        if "```" in instructions or "#" in instructions or "*" in instructions:
            console.print(Markdown(instructions))
        else:
            console.print(instructions)
        
        console.print()
        
        # Show expected command if requested (for debugging or after attempts)
        if show_expected:
            console.print("[bold yellow]Expected Command:[/bold yellow]")
            console.print(Syntax(expected_command, "powershell", theme="monokai", line_numbers=False))
    else:
        # Plain text fallback
        print(f"\nStep {step_number}:")
        print(instructions)
        
        if show_expected:
            print(f"\nExpected Command: {expected_command}")


def prompt_for_command() -> str:
    """Prompt the user to enter a command."""
    try:
        if RICH_AVAILABLE:
            console.print()
            console.print("[bold cyan]Enter your command:[/bold cyan]")
            user_input = Prompt.ask("> ")
            return user_input.strip()
        else:
            print("\nEnter your command:")
            return input("> ").strip()
    except KeyboardInterrupt:
        if RICH_AVAILABLE:
            console.print("\n[bold yellow]Operation cancelled by user.[/bold yellow]")
        else:
            print("\nOperation cancelled by user.")
        sys.exit(0)


def check_command(user_input: str, expected_command: str) -> Tuple[bool, str]:
    """
    Check if the user's input matches the expected command.
    
    Returns:
        Tuple[bool, str]: (is_correct, feedback_message)
    """
    # Simple exact match comparison (case-insensitive)
    is_correct = user_input.lower() == expected_command.lower()
    
    if is_correct:
        return True, "Correct! Great job!"
    else:
        return False, "That's not quite right. Try again!"


def run_tutorial_step(step: Dict[str, Any], step_number: int) -> bool:
    """
    Run an interactive tutorial step.
    
    Args:
        step: The step data
        step_number: The step number to display
        
    Returns:
        bool: True if the step was completed successfully, False otherwise
    """
    if not step:
        if RICH_AVAILABLE:
            console.print("[yellow]This step is empty or not available.[/yellow]")
        else:
            print("This step is empty or not available.")
        return False
    
    expected_command = step.get('expectedCommand', '')
    hint = step.get('hint', 'Try reading the instructions carefully.')
    
    # Display step instructions (without showing the expected command)
    display_step(step, step_number)
    
    # First attempt
    user_input = prompt_for_command()
    is_correct, feedback = check_command(user_input, expected_command)
    
    if RICH_AVAILABLE:
        if is_correct:
            console.print(f"\n[bold green]{feedback}[/bold green]")
            return True
        else:
            console.print(f"\n[bold red]{feedback}[/bold red]")
            console.print(f"[yellow]Hint: {hint}[/yellow]")
    else:
        if is_correct:
            print(f"\n{feedback}")
            return True
        else:
            print(f"\n{feedback}")
            print(f"Hint: {hint}")
    
    # Second attempt
    user_input = prompt_for_command()
    is_correct, feedback = check_command(user_input, expected_command)
    
    if RICH_AVAILABLE:
        if is_correct:
            console.print(f"\n[bold green]{feedback}[/bold green]")
            return True
        else:
            console.print(f"\n[bold red]{feedback}[/bold red]")
            console.print("[yellow]Let's see the expected command:[/yellow]")
            console.print(Syntax(expected_command, "powershell", theme="monokai", line_numbers=False))
    else:
        if is_correct:
            print(f"\n{feedback}")
            return True
        else:
            print(f"\n{feedback}")
            print(f"Expected command: {expected_command}")
    
    return is_correct


def prompt_for_tutorial_selection(tutorials: List[Dict[str, Any]]) -> Optional[Dict[str, Any]]:
    """Prompt the user to select a tutorial from the list."""
    if not tutorials:
        return None
    
    try:
        if RICH_AVAILABLE:
            selection = Prompt.ask(
                "\n[bold green]Enter the number of the tutorial you want to view[/bold green]",
                default="1"
            )
        else:
            selection = input("\nEnter the number of the tutorial you want to view (or press Enter for #1): ") or "1"
        
        try:
            index = int(selection) - 1
            if 0 <= index < len(tutorials):
                return tutorials[index]
            else:
                if RICH_AVAILABLE:
                    console.print(f"[bold red]Invalid selection: {selection}. Please enter a number between 1 and {len(tutorials)}.[/bold red]")
                else:
                    print(f"Invalid selection: {selection}. Please enter a number between 1 and {len(tutorials)}.")
                return None
        except ValueError:
            if RICH_AVAILABLE:
                console.print(f"[bold red]Invalid input: {selection}. Please enter a number.[/bold red]")
            else:
                print(f"Invalid input: {selection}. Please enter a number.")
            return None
    except KeyboardInterrupt:
        if RICH_AVAILABLE:
            console.print("\n[bold yellow]Operation cancelled by user.[/bold yellow]")
        else:
            print("\nOperation cancelled by user.")
        sys.exit(0)


def run_tutorial(tutorial: Dict[str, Any], tutorial_client: TutorialClient) -> None:
    """Run an interactive tutorial with all its steps."""
    if not tutorial:
        if RICH_AVAILABLE:
            console.print("[bold red]Tutorial not found or could not be loaded.[/bold red]")
        else:
            print("Tutorial not found or could not be loaded.")
        return
    
    steps = tutorial.get('steps', [])
    
    if not steps:
        if RICH_AVAILABLE:
            console.print("[yellow]This tutorial has no interactive steps.[/yellow]")
        else:
            print("This tutorial has no interactive steps.")
        return
    
    # Display tutorial header
    display_tutorial_header(tutorial)
    
    # Run the first step
    first_step = steps[0]
    success = run_tutorial_step(first_step, 1)
    
    if success:
        if RICH_AVAILABLE:
            console.print("\n[bold green]Congratulations on completing the first step![/bold green]")
            console.print("[italic]In a full tutorial, you would continue with more steps.[/italic]")
            
            # Report progress to the API
            tutorial_client.complete_tutorial(tutorial.get('id'), 10)
        else:
            print("\nCongratulations on completing the first step!")
            print("In a full tutorial, you would continue with more steps.")


def main():
    """Main entry point for the CLI application."""
    # Define API base URL
    from utils.config import API_BASE_URL
    
    print_header("CmdShiftLearn CLI Agent")
    
    # Authenticate with API key
    success, api_key, error = login()
    if not success or not api_key:
        if RICH_AVAILABLE:
            console.print("[bold red]Authentication required to use CmdShiftLearn CLI.[/bold red]")
            if error:
                console.print(f"[red]{error}[/red]")
        else:
            print("Authentication required to use CmdShiftLearn CLI.")
            if error:
                print(error)
        sys.exit(1)
    
    if RICH_AVAILABLE:
        console.print("[italic]Connecting to CmdShiftLearn API...[/italic]")
    else:
        print("Connecting to CmdShiftLearn API...\n")
    
    # Create tutorial client with the API key
    tutorial_client = TutorialClient(api_key)
    
    try:
        # Fetch and display the list of tutorials
        tutorials = tutorial_client.get_tutorials()
        
        if not tutorials:
            if RICH_AVAILABLE:
                console.print("[red]Failed to retrieve tutorials from the API. Please check your API key.[/red]")
            else:
                print("Failed to retrieve tutorials from the API. Please check your API key.")
            sys.exit(1)
            
        print_header("Available Tutorials")
        print_tutorials(tutorials)
        
        # Prompt user to select a tutorial
        selected_tutorial = prompt_for_tutorial_selection(tutorials)
        if selected_tutorial:
            # Fetch the full tutorial details
            tutorial_id = selected_tutorial.get('id')
            if RICH_AVAILABLE:
                console.print(f"[italic]Loading tutorial [bold]{tutorial_id}[/bold]...[/italic]")
            else:
                print(f"\nLoading tutorial {tutorial_id}...")
            
            full_tutorial = tutorial_client.get_tutorial_by_id(tutorial_id)
            
            if full_tutorial:
                # Run the interactive tutorial
                run_tutorial(full_tutorial, tutorial_client)
            else:
                if RICH_AVAILABLE:
                    console.print(f"[red]Failed to load tutorial {tutorial_id}. Please try again later.[/red]")
                else:
                    print(f"Failed to load tutorial {tutorial_id}. Please try again later.")
        
    except KeyboardInterrupt:
        if RICH_AVAILABLE:
            console.print("\n[bold yellow]Operation cancelled by user.[/bold yellow]")
        else:
            print("\nOperation cancelled by user.")
        sys.exit(0)
    except Exception as e:
        if RICH_AVAILABLE:
            console.print(f"[bold red]Error:[/bold red] {str(e)}")
        else:
            print(f"Error: {str(e)}")


if __name__ == "__main__":
    main()