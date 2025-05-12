#!/usr/bin/env python3
"""
CmdShiftLearn CLI Agent - Enhanced Python Edition

A command-line tool for interacting with the CmdShiftLearn platform
with rich animated terminal experience.
"""

import sys
import os
import time
import logging
import random
from typing import List, Dict, Any, Optional, Tuple

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('main')

# Import the enhanced animated UI
from terminal.animated_ui import AnimatedTerminalUI
from api.tutorials import TutorialClient
from api.auth import login, load_api_key
import powershell.executor as ps_executor
from utils.config import API_BASE_URL

def check_command(user_input: str, expected_command: str, validation_type: str = 'exact', 
                 case_sensitive: bool = False, output_check: str = None) -> Tuple[bool, str]:
    """
    Check if the user's input matches the expected command.
    
    Args:
        user_input: The user's input command
        expected_command: The expected command
        validation_type: Type of validation ('exact', 'contains', 'output')
        case_sensitive: Whether to perform case-sensitive matching
        output_check: Optional pattern to match against command output
        
    Returns:
        Tuple[bool, str]: (is_correct, feedback_message)
    """
    if validation_type == 'any':
        # Any input is valid
        return True, "Great! Let's continue."
    
    elif validation_type == 'exact':
        # Exact match (with case sensitivity option)
        if case_sensitive:
            is_correct = user_input == expected_command
        else:
            is_correct = user_input.lower() == expected_command.lower()
        
        if is_correct:
            return True, "Correct! Great job!"
        else:
            return False, "That's not quite right. Try again!"
    
    elif validation_type == 'contains':
        # Check if input contains expected command
        if case_sensitive:
            is_correct = expected_command in user_input
        else:
            is_correct = expected_command.lower() in user_input.lower()
        
        if is_correct:
            return True, "Good! Your command contains the expected pattern."
        else:
            return False, "Your command doesn't include the expected pattern."
    
    elif validation_type == 'output' and output_check:
        # Execute command and check output
        success_result, stdout, stderr = ps_executor.execute_powershell_command(user_input)
        is_correct = output_check.lower() in stdout.lower() if success_result else False
        
        if is_correct:
            return True, "Great! Your command produced the expected output."
        else:
            return False, "Your command didn't produce the expected output."
    
    else:
        # Fall back to basic validation
        is_correct = user_input.lower() == expected_command.lower()
        
        if is_correct:
            return True, "Correct!"
        else:
            return False, "That's not quite right. Try again!"


def run_animated_tutorial_step(ui: AnimatedTerminalUI, step: Dict[str, Any], 
                            step_number: int, total_steps: int, tutorial_id: str) -> bool:
    """
    Run an interactive tutorial step with animations.
    
    Args:
        ui: The animated terminal UI
        step: The step data
        step_number: The step number to display
        total_steps: Total number of steps in the tutorial
        tutorial_id: ID of the current tutorial
        
    Returns:
        bool: True if the step was completed successfully, False otherwise
    """
    if not step:
        ui.display_error("This step is empty or not available.")
        return False
    
    # Extract step details
    step_id = step.get('id', f'step{step_number}')
    expected_command = step.get('expectedCommand', '')
    hint = step.get('hint', 'Try reading the instructions carefully.')
    ascii_art = step.get('ascii_art', None)
    
    # Get validation settings
    validation = step.get('validation', {})
    if isinstance(validation, dict):
        validation_type = validation.get('type', 'exact')
        case_sensitive = validation.get('caseSensitive', False)
        output_check = validation.get('outputMatch', None)
    else:
        # Fallback for older format
        validation_type = 'exact'
        case_sensitive = False
        output_check = None
    
    # Display step with animations
    ui.display_step(step, step_number, total_steps)
    
    # Set up for attempts
    max_attempts = 3
    attempts = 0
    success = False
    
    # Command input loop with animations
    while attempts < max_attempts and not success:
        # Get command input with animation
        user_input = ui.get_command_input(step)
        
        # Check for special commands
        if user_input.lower() == 'exit' or user_input.lower() == 'quit':
            return False
        elif user_input.lower() == 'hint':
            ui.display_hint(hint)
            continue
        elif user_input.lower() == 'skip':
            ui.display_expected_command(expected_command)
            return True
        
        # Validate the command
        is_correct, feedback = check_command(
            user_input, 
            expected_command, 
            validation_type, 
            case_sensitive, 
            output_check
        )
        
        # Display animated feedback
        ui.display_feedback(is_correct, feedback)
        
        if is_correct:
            success = True
            
            # Report step completion to the API
            tutorial_client = TutorialClient()
            tutorial_client.report_step_completion(tutorial_id, step_id, step.get('xp', 10))
            
            # Show XP animation
            ui.animate_xp_gain(step.get('xp', 10))
            
            # Display ASCII art if available
            if ascii_art:
                ui.console.print(ascii_art, style="bold green")
                time.sleep(1)
            
            # Celebrate step completion with animation
            ui.celebrate_step_completion()
        else:
            attempts += 1
            
            # If max attempts reached, show the expected command
            if attempts >= max_attempts:
                ui.display_expected_command(expected_command)
                
                # Ask if the user wants to continue
                ui.console.print()
                ui.console.print("[yellow]Would you like to continue to the next step? (y/n)[/yellow]")
                
                # Get user choice with animated prompt
                user_choice = input("> ").strip().lower()
                
                if user_choice != 'y' and user_choice != 'yes':
                    return False
                
                # Slight pause before continuing
                time.sleep(0.5)
                return True
    
    return True


def run_animated_tutorial(ui: AnimatedTerminalUI, tutorial: Dict[str, Any]) -> None:
    """
    Run an interactive tutorial with all its steps and animations.
    
    Args:
        ui: The animated terminal UI
        tutorial: The tutorial data
    """
    if not tutorial:
        ui.display_error("Tutorial not found or could not be loaded.")
        return
    
    steps = tutorial.get('steps', [])
    
    if not steps:
        ui.display_error("This tutorial has no interactive steps.")
        return
    
    # Display tutorial header with animation
    ui.display_tutorial_header(tutorial)
    
    # Add a slight pause before starting the tutorial
    time.sleep(1)
    
    total_xp = 0
    
    # Run each step with animations
    for i, step in enumerate(steps, 1):
        if not run_animated_tutorial_step(ui, step, i, len(steps), tutorial.get('id')):
            # Step failed or user quit
            ui.display_error("Tutorial stopped. You can try again later.")
            return
        
        # Add XP for the step
        step_xp = step.get('xp', 10)
        total_xp += step_xp
    
    # Report tutorial completion to the API
    tutorial_client = TutorialClient()
    tutorial_client.report_tutorial_completion(tutorial.get('id'), total_xp)
    
    # Celebrate tutorial completion with animations
    ui.celebrate_tutorial_completion(tutorial.get('title', 'Tutorial'), total_xp)


def animated_main():
    """Enhanced main entry point with animated UI for the CLI application."""
    # Initialize the animated UI
    ui = AnimatedTerminalUI()
    
    # Clear screen and show welcome message with animations
    ui.clear_screen()
    ui.show_welcome_screen()
    
    # Header animation
    ui.console.rule("[bold blue]CmdShiftLearn CLI Agent[/bold blue]")
    
    # Authenticate with API key animation
    ui.console.print()
    ui.animated_rich_text("Authenticating...", style="bold blue", delay=0.02)
    
    success, api_key, error = login()
    if not success or not api_key:
        ui.display_error("Authentication required to use CmdShiftLearn CLI.")
        if error:
            ui.console.print(f"[red]{error}[/red]")
        sys.exit(1)
    
    # Show connection info animation
    ui.animated_rich_text(
        f"Connecting to CmdShiftLearn API at: {API_BASE_URL}", 
        style="italic cyan", 
        delay=0.01
    )
    ui.console.print()
    
    # Create tutorial client with the API key
    tutorial_client = TutorialClient(api_key)
    
    try:
        # Fetch tutorials with loading animation
        tutorials = ui.display_loading(
            "Fetching tutorials from the API...",
            tutorial_client.get_tutorials
        )
        
        if not tutorials:
            ui.display_error("Failed to retrieve tutorials from the API. Please check your API key.")
            sys.exit(1)
        
        # Display available tutorials with animation
        ui.console.rule("[bold blue]Available Tutorials[/bold blue]")
        ui.console.print()
        
        # Create and display tutorial table with animations
        table_title = "Choose a tutorial to begin your learning journey"
        ui.animated_rich_text(table_title, style="bold", delay=0.01)
        ui.console.print()
        
        # Display tutorials one by one with a subtle animation
        for i, tutorial in enumerate(tutorials, 1):
            title = tutorial.get('title', 'Untitled')
            difficulty = tutorial.get('difficulty', 'Unknown')
            id = tutorial.get('id', 'unknown')
            
            # Create a colorful display based on difficulty
            if difficulty.lower() == 'beginner':
                diff_color = "green"
            elif difficulty.lower() == 'intermediate':
                diff_color = "yellow"
            else:
                diff_color = "red"
            
            tutorial_text = f"[cyan]{i}.[/cyan] [bold]{title}[/bold] [[{diff_color}]{difficulty}[/{diff_color}]] (ID: {id})"
            ui.animated_rich_text(tutorial_text, delay=0.01)
            time.sleep(0.1)  # Slight pause between tutorials
        
        # Prompt for tutorial selection with animation
        ui.console.print()
        prompt_text = "[bold yellow]Enter the number of the tutorial you want to view:[/bold yellow]"
        ui.animated_rich_text(prompt_text, delay=0.01)
        selection = input("> ").strip()
        
        try:
            index = int(selection) - 1
            if 0 <= index < len(tutorials):
                selected_tutorial = tutorials[index]
                
                # Get tutorial ID
                tutorial_id = selected_tutorial.get('id')
                
                # Loading animation for tutorial
                ui.console.print()
                loading_text = f"Loading tutorial [bold]{tutorial_id}[/bold]..."
                ui.animated_rich_text(loading_text, style="italic", delay=0.02)
                
                # Fetch the full tutorial details with loading animation
                full_tutorial = ui.display_loading(
                    f"Fetching tutorial data...",
                    lambda: tutorial_client.get_tutorial_by_id(tutorial_id)
                )
                
                if full_tutorial:
                    # Run the interactive tutorial with animations
                    run_animated_tutorial(ui, full_tutorial)
                else:
                    ui.display_error(f"Failed to load tutorial {tutorial_id}. Please try again later.")
            else:
                ui.display_error(f"Invalid selection: {selection}. Please enter a number between 1 and {len(tutorials)}.")
        except ValueError:
            ui.display_error(f"Invalid input: {selection}. Please enter a number.")
            
    except KeyboardInterrupt:
        ui.console.print("\n[bold yellow]Operation cancelled by user.[/bold yellow]")
        sys.exit(0)
    except Exception as e:
        ui.display_error(f"Unexpected error: {str(e)}")
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)


def main():
    """Main entry point dispatcher."""
    # Use the enhanced animated version by default
    animated_main()


if __name__ == "__main__":
    main()
