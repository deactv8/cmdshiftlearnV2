#!/usr/bin/env python3
"""
CmdShiftLearn - CLI Launcher with animated UI

This script provides the command-line interface for CmdShiftLearn
with enhanced animated terminal experience.
"""

import sys
import os
import argparse
import logging
from typing import List, Optional

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger('cmdagent')

def create_parser() -> argparse.ArgumentParser:
    """
    Create command-line argument parser.
    
    Returns:
        argparse.ArgumentParser: Configured parser
    """
    parser = argparse.ArgumentParser(description="CmdShiftLearn - Interactive PowerShell Tutorial Platform")
    subparsers = parser.add_subparsers(dest="command", help="Command to run")
    
    # Tutorial command
    tutorial_parser = subparsers.add_parser("tutorial", help="Tutorial commands")
    tutorial_subparsers = tutorial_parser.add_subparsers(dest="tutorial_command", help="Tutorial subcommand")
    
    # Tutorial list command
    list_parser = tutorial_subparsers.add_parser("list", help="List available tutorials")
    
    # Tutorial start command
    start_parser = tutorial_subparsers.add_parser("start", help="Start a tutorial")
    start_parser.add_argument("tutorial_id", help="ID of the tutorial to start")
    
    return parser

def process_args(args: Optional[List[str]] = None) -> None:
    """
    Process command-line arguments and dispatch to the appropriate handler.
    
    Args:
        args: Command-line arguments (defaults to sys.argv[1:])
    """
    parser = create_parser()
    parsed_args = parser.parse_args(args)
    
    # Import UI and other modules only when needed
    from terminal.animated_ui import AnimatedTerminalUI
    from api.tutorials import TutorialClient
    from api.auth import login, load_api_key
    from animated_main import run_animated_tutorial
    
    # Initialize the UI
    ui = AnimatedTerminalUI()
    
    # Clear screen and show welcome
    ui.clear_screen()
    ui.show_welcome_screen()
    
    # Authenticate
    success, api_key, error = login()
    if not success or not api_key:
        ui.display_error("Authentication required to use CmdShiftLearn CLI.")
        if error:
            ui.console.print(f"[red]{error}[/red]")
        return
    
    # Create tutorial client
    tutorial_client = TutorialClient(api_key)
    
    # Process commands
    if not parsed_args.command:
        # If no command is specified, run the main interface
        from animated_main import animated_main
        animated_main()
        return
    
    if parsed_args.command == "tutorial":
        if not parsed_args.tutorial_command or parsed_args.tutorial_command == "list":
            # List tutorials
            ui.console.rule("[bold blue]Available Tutorials[/bold blue]")
            ui.console.print()
            
            # Fetch tutorials with loading animation
            tutorials = ui.display_loading(
                "Fetching tutorials from the API...",
                tutorial_client.get_tutorials
            )
            
            if not tutorials:
                ui.display_error("Failed to retrieve tutorials from the API.")
                return
            
            # Display tutorials with animation
            ui.display_tutorials_animated(tutorials)
            
        elif parsed_args.tutorial_command == "start":
            # Start a specific tutorial
            tutorial_id = parsed_args.tutorial_id
            ui.console.print(f"Starting tutorial: [bold]{tutorial_id}[/bold]")
            
            # Fetch the tutorial with loading animation
            tutorial = ui.display_loading(
                f"Loading tutorial {tutorial_id}...",
                lambda: tutorial_client.get_tutorial_by_id(tutorial_id)
            )
            
            if not tutorial:
                ui.display_error(f"Tutorial '{tutorial_id}' not found or could not be loaded.")
                return
            
            # Run the tutorial with animations
            run_animated_tutorial(ui, tutorial)
    else:
        parser.print_help()

def main() -> None:
    """Main entry point for the CLI."""
    try:
        process_args()
    except KeyboardInterrupt:
        print("\nOperation cancelled by user.")
        sys.exit(0)
    except Exception as e:
        print(f"Error: {str(e)}")
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        sys.exit(1)

if __name__ == "__main__":
    main()
