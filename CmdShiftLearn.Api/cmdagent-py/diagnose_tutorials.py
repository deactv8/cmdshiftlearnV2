#!/usr/bin/env python3
"""
Diagnostic script for tutorial loading in CmdShiftLearn.

This script helps diagnose issues with tutorial loading by checking:
1. Local tutorial files
2. API tutorial endpoints
3. Comparing local and API tutorials
"""

import os
import sys
import json
import yaml
import httpx
import logging
from pathlib import Path
from rich.console import Console
from rich.table import Table
from rich.panel import Panel
from rich.markdown import Markdown
from rich.tree import Tree
from rich.syntax import Syntax
from typing import List, Dict, Any, Optional, Tuple

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler("tutorial_diagnosis.log"),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger("tutorial_diagnosis")

# Initialize Rich console
console = Console()

# Configuration
from utils.config import API_BASE_URL, DATA_DIR
from api.auth import login, load_api_key


def check_local_tutorials() -> List[Dict[str, Any]]:
    """
    Check local tutorial files.
    
    Returns:
        List[Dict[str, Any]]: List of local tutorial metadata
    """
    console.rule("[bold blue]Checking Local Tutorial Files[/bold blue]")
    
    local_tutorials = []
    
    # Define paths to check
    paths_to_check = [
        Path(DATA_DIR) / "content" / "tutorials" / "beginner",
        Path(DATA_DIR) / "content" / "tutorials",
        Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials" / "beginner",
        Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials",
    ]
    
    # Create a tree for displaying tutorial files
    file_tree = Tree("[bold]Tutorial Files[/bold]")
    
    # Check each path
    for path in paths_to_check:
        path_node = file_tree.add(f"[blue]{path}[/blue]")
        
        if not path.exists():
            path_node.add(f"[red]Directory does not exist[/red]")
            continue
        
        # Find all YAML files
        yaml_files = list(path.glob("*.yaml")) + list(path.glob("*.yml"))
        
        if not yaml_files:
            path_node.add(f"[yellow]No YAML files found[/yellow]")
            continue
        
        # Load each tutorial file
        for yaml_file in yaml_files:
            try:
                with open(yaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                    tutorial_data = yaml.safe_load(content)
                    
                    if tutorial_data and isinstance(tutorial_data, dict):
                        tutorial_id = tutorial_data.get("id", yaml_file.stem)
                        tutorial_title = tutorial_data.get("title", "Untitled")
                        
                        # Add to the tree
                        file_info = f"[green]{yaml_file.name}[/green] - {tutorial_id}: {tutorial_title}"
                        path_node.add(file_info)
                        
                        # Add to the list
                        local_tutorials.append({
                            "id": tutorial_id,
                            "title": tutorial_title,
                            "description": tutorial_data.get("description", ""),
                            "difficulty": tutorial_data.get("difficulty", "Beginner"),
                            "xp": tutorial_data.get("xpTotal", 0),
                            "localPath": str(yaml_file),
                            "steps": len(tutorial_data.get("steps", []))
                        })
                    else:
                        path_node.add(f"[yellow]{yaml_file.name} - Invalid YAML format[/yellow]")
                        
            except Exception as e:
                path_node.add(f"[red]{yaml_file.name} - Error: {str(e)}[/red]")
                logger.error(f"Error loading tutorial file {yaml_file}: {str(e)}")
    
    # Display the tree
    console.print(file_tree)
    
    # Display a table of local tutorials
    if local_tutorials:
        console.print()
        console.print(f"[green]Found {len(local_tutorials)} local tutorials[/green]")
        
        table = Table(title="Local Tutorials")
        table.add_column("ID", style="cyan")
        table.add_column("Title", style="green")
        table.add_column("Steps", style="magenta")
        table.add_column("Difficulty", style="yellow")
        table.add_column("Path", style="blue")
        
        for tutorial in local_tutorials:
            table.add_row(
                tutorial["id"],
                tutorial["title"],
                str(tutorial["steps"]),
                tutorial["difficulty"],
                tutorial["localPath"]
            )
        
        console.print(table)
    else:
        console.print()
        console.print("[red]No local tutorials found[/red]")
    
    return local_tutorials


def check_api_tutorials(api_key: str) -> List[Dict[str, Any]]:
    """
    Check API tutorial endpoints.
    
    Args:
        api_key: API key for authentication
        
    Returns:
        List[Dict[str, Any]]: List of API tutorial metadata
    """
    console.rule("[bold blue]Checking API Tutorial Endpoints[/bold blue]")
    
    api_tutorials = []
    
    # Define headers
    headers = {
        "Authorization": f"ApiKey {api_key}",
        "Accept": "application/json"
    }
    
    # Check /api/tutorials endpoint
    console.print(f"[bold]Testing API endpoint:[/bold] {API_BASE_URL}/tutorials")
    
    try:
        with httpx.Client(timeout=10.0) as client:
            response = client.get(f"{API_BASE_URL}/tutorials", headers=headers)
        
        console.print(f"Response status code: {response.status_code}")
        
        if response.status_code == 200:
            tutorials = response.json()
            console.print(f"[green]Successfully fetched {len(tutorials)} tutorials from API[/green]")
            
            # Save the tutorials for later comparison
            api_tutorials = tutorials
            
            # Display tutorial table
            table = Table(title="API Tutorials")
            table.add_column("ID", style="cyan")
            table.add_column("Title", style="green")
            table.add_column("Difficulty", style="yellow")
            
            for tutorial in tutorials:
                table.add_row(
                    tutorial.get("id", "Unknown"),
                    tutorial.get("title", "Unknown"),
                    tutorial.get("difficulty", "Unknown")
                )
            
            console.print(table)
            
            # Now check individual tutorials
            console.print()
            console.print("[bold]Testing individual tutorial endpoints...[/bold]")
            
            for i, tutorial in enumerate(tutorials[:3]):  # Check first 3 for brevity
                tutorial_id = tutorial.get("id")
                console.print(f"[bold]Testing tutorial endpoint for ID:[/bold] {tutorial_id}")
                
                try:
                    with httpx.Client(timeout=10.0) as client:
                        tutorial_response = client.get(f"{API_BASE_URL}/tutorials/{tutorial_id}", headers=headers)
                    
                    console.print(f"Response status code: {tutorial_response.status_code}")
                    
                    if tutorial_response.status_code == 200:
                        tutorial_data = tutorial_response.json()
                        console.print(f"[green]Successfully fetched tutorial: {tutorial_data.get('title')}[/green]")
                        console.print(f"Steps: {len(tutorial_data.get('steps', []))}")
                    else:
                        console.print(f"[red]Error fetching tutorial: {tutorial_response.text}[/red]")
                
                except Exception as e:
                    console.print(f"[red]Error requesting tutorial: {str(e)}[/red]")
            
        else:
            console.print(f"[red]Error: {response.status_code} - {response.text}[/red]")
    
    except Exception as e:
        console.print(f"[red]Error connecting to API: {str(e)}[/red]")
    
    return api_tutorials


def compare_tutorials(local_tutorials: List[Dict[str, Any]], api_tutorials: List[Dict[str, Any]]) -> None:
    """
    Compare local and API tutorials to find discrepancies.
    
    Args:
        local_tutorials: List of local tutorial metadata
        api_tutorials: List of API tutorial metadata
    """
    console.rule("[bold blue]Comparing Local and API Tutorials[/bold blue]")
    
    if not local_tutorials and not api_tutorials:
        console.print("[red]No tutorials to compare[/red]")
        return
    
    # Create sets of tutorial IDs for comparison
    local_ids = {t["id"] for t in local_tutorials}
    api_ids = {t.get("id") for t in api_tutorials}
    
    # Find missing tutorials
    missing_in_api = local_ids - api_ids
    missing_in_local = api_ids - local_ids
    common_ids = local_ids.intersection(api_ids)
    
    # Display missing tutorials
    if missing_in_api:
        console.print("[yellow]Tutorials found locally but missing in API:[/yellow]")
        for tutorial_id in missing_in_api:
            local_tutorial = next((t for t in local_tutorials if t["id"] == tutorial_id), None)
            if local_tutorial:
                console.print(f"  - [cyan]{tutorial_id}[/cyan]: {local_tutorial['title']} "
                              f"(File: {local_tutorial['localPath']})")
    
    if missing_in_local:
        console.print("[yellow]Tutorials found in API but missing locally:[/yellow]")
        for tutorial_id in missing_in_local:
            api_tutorial = next((t for t in api_tutorials if t.get("id") == tutorial_id), None)
            if api_tutorial:
                console.print(f"  - [cyan]{tutorial_id}[/cyan]: {api_tutorial.get('title', 'Unknown')}")
    
    # Compare common tutorials
    if common_ids:
        console.print(f"[green]Found {len(common_ids)} tutorials in both local files and API[/green]")
        
        table = Table(title="Tutorial Comparison")
        table.add_column("ID", style="cyan")
        table.add_column("Title (Local)", style="green")
        table.add_column("Title (API)", style="blue")
        table.add_column("Steps (Local)", style="green")
        table.add_column("Found in API", style="magenta")
        
        for tutorial_id in common_ids:
            local_tutorial = next((t for t in local_tutorials if t["id"] == tutorial_id), {})
            api_tutorial = next((t for t in api_tutorials if t.get("id") == tutorial_id), {})
            
            local_title = local_tutorial.get("title", "Unknown")
            api_title = api_tutorial.get("title", "Unknown")
            local_steps = str(local_tutorial.get("steps", 0))
            
            titles_match = local_title == api_title
            status = "[green]✓[/green]" if titles_match else "[yellow]≠[/yellow]"
            
            table.add_row(
                tutorial_id,
                local_title,
                api_title,
                local_steps,
                status
            )
        
        console.print(table)
    
    # Provide recommendations
    console.print()
    console.print("[bold]Recommendations:[/bold]")
    
    if missing_in_api and not api_tutorials:
        console.print("[red]The API is not returning any tutorials. Check the following:[/red]")
        console.print("  1. Verify that the backend API is running correctly")
        console.print("  2. Check if GitHub integration is properly configured in appsettings.json")
        console.print("  3. Ensure the GitHub token has access to the repository")
        console.print("  4. Check if the repository and branch exist")
        console.print("  5. Verify the tutorials path is correct in the configuration")
    elif missing_in_api:
        console.print("[yellow]Some local tutorials are missing from the API. Consider:[/yellow]")
        console.print("  1. Checking if the GitHub tutorials path is correctly set to 'tutorials/beginner'")
        console.print("  2. Verifying that the tutorial files exist in the GitHub repository")
        console.print("  3. Ensuring the tutorial IDs match the filenames or YAML 'id' fields")
    
    if missing_in_local:
        console.print("[yellow]Some API tutorials are not found locally. Consider:[/yellow]")
        console.print("  1. Syncing your local files with the GitHub repository")
        console.print("  2. Checking if the GitHub API is returning additional tutorials")
    
    if not missing_in_api and not missing_in_local and common_ids:
        console.print("[green]All tutorials are present in both local files and the API![/green]")
        console.print("If you're still experiencing issues, check for content issues or parsing problems.")


def diagnose_specific_tutorial(api_key: str, tutorial_id: str) -> None:
    """
    Diagnose issues with a specific tutorial.
    
    Args:
        api_key: API key for authentication
        tutorial_id: ID of the tutorial to diagnose
    """
    console.rule(f"[bold blue]Diagnosing Tutorial: {tutorial_id}[/bold blue]")
    
    # Check local file first
    console.print("[bold]Checking local files...[/bold]")
    
    local_tutorial = None
    local_paths = [
        Path(DATA_DIR) / "content" / "tutorials" / "beginner",
        Path(DATA_DIR) / "content" / "tutorials",
        Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials" / "beginner",
        Path(os.path.dirname(os.path.dirname(__file__))) / "data" / "content" / "tutorials",
    ]
    
    # Find local tutorial files
    possible_files = []
    
    for path in local_paths:
        if path.exists():
            # Look for exact filename matches
            for ext in [".yaml", ".yml"]:
                file_path = path / f"{tutorial_id}{ext}"
                if file_path.exists():
                    possible_files.append(file_path)
            
            # Look for files with matching ID in content
            for yaml_file in list(path.glob("*.yaml")) + list(path.glob("*.yml")):
                try:
                    with open(yaml_file, 'r', encoding='utf-8') as f:
                        content = f.read()
                        tutorial_data = yaml.safe_load(content)
                        
                        if tutorial_data and isinstance(tutorial_data, dict):
                            if tutorial_data.get("id") == tutorial_id:
                                possible_files.append(yaml_file)
                except Exception:
                    pass
    
    if possible_files:
        console.print(f"[green]Found {len(possible_files)} possible local tutorial files:[/green]")
        
        for i, file_path in enumerate(possible_files):
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    tutorial_data = yaml.safe_load(content)
                    
                    local_tutorial = tutorial_data
                    
                    console.print(f"[bold]Local file {i+1}:[/bold] {file_path}")
                    console.print(f"  ID: {tutorial_data.get('id', 'Not specified')}")
                    console.print(f"  Title: {tutorial_data.get('title', 'Not specified')}")
                    console.print(f"  Steps: {len(tutorial_data.get('steps', []))}")
                    
                    # Display a preview of the first step
                    if tutorial_data.get("steps"):
                        first_step = tutorial_data["steps"][0]
                        console.print("[bold]First step preview:[/bold]")
                        console.print(f"  Title: {first_step.get('title', 'Not specified')}")
                        console.print(f"  Expected Command: {first_step.get('expectedCommand', 'Not specified')}")
            except Exception as e:
                console.print(f"[red]Error reading file {file_path}: {str(e)}[/red]")
    else:
        console.print("[red]No local tutorial files found for ID: {tutorial_id}[/red]")
    
    # Check API
    console.print()
    console.print("[bold]Checking API...[/bold]")
    
    headers = {
        "Authorization": f"ApiKey {api_key}",
        "Accept": "application/json"
    }
    
    try:
        with httpx.Client(timeout=10.0) as client:
            response = client.get(f"{API_BASE_URL}/tutorials/{tutorial_id}", headers=headers)
        
        console.print(f"Response status code: {response.status_code}")
        
        if response.status_code == 200:
            api_tutorial = response.json()
            console.print(f"[green]Successfully fetched tutorial from API: {api_tutorial.get('title')}[/green]")
            console.print(f"  ID: {api_tutorial.get('id', 'Not specified')}")
            console.print(f"  Steps: {len(api_tutorial.get('steps', []))}")
            
            # Compare with local
            if local_tutorial:
                console.print()
                console.print("[bold]Comparing local and API versions:[/bold]")
                
                # Check ID match
                local_id = local_tutorial.get("id", "Not specified")
                api_id = api_tutorial.get("id", "Not specified")
                
                if local_id == api_id:
                    console.print("[green]✓ IDs match[/green]")
                else:
                    console.print(f"[red]✗ ID mismatch: Local={local_id}, API={api_id}[/red]")
                
                # Check title match
                local_title = local_tutorial.get("title", "Not specified")
                api_title = api_tutorial.get("title", "Not specified")
                
                if local_title == api_title:
                    console.print("[green]✓ Titles match[/green]")
                else:
                    console.print(f"[red]✗ Title mismatch: Local={local_title}, API={api_title}[/red]")
                
                # Check steps count
                local_steps = len(local_tutorial.get("steps", []))
                api_steps = len(api_tutorial.get("steps", []))
                
                if local_steps == api_steps:
                    console.print(f"[green]✓ Step counts match: {local_steps}[/green]")
                else:
                    console.print(f"[red]✗ Step count mismatch: Local={local_steps}, API={api_steps}[/red]")
        else:
            console.print(f"[red]Error fetching tutorial from API: {response.text}[/red]")
    
    except Exception as e:
        console.print(f"[red]Error connecting to API: {str(e)}[/red]")
    
    # Provide recommendations
    console.print()
    console.print("[bold]Recommendations:[/bold]")
    
    if not possible_files:
        console.print("[yellow]No local files found. Consider:[/yellow]")
        console.print("  1. Creating a local tutorial file with the correct ID")
        console.print("  2. Checking if the ID is spelled correctly")
    
    if local_tutorial and not api_tutorial:
        console.print("[yellow]Tutorial exists locally but not in API. Consider:[/yellow]")
        console.print("  1. Verifying the GitHub repository contains this tutorial")
        console.print("  2. Checking if the ID in the YAML file matches what you're requesting")
        console.print("  3. Ensuring the GitHub path is correctly set to point to the tutorial folder")
    
    if local_tutorial and api_tutorial and local_tutorial.get("id") != api_tutorial.get("id"):
        console.print("[red]ID mismatch between local and API versions. This is likely the main issue.[/red]")
        console.print("  1. Make sure the 'id' field in the YAML file matches exactly what the cmdagent.py is requesting")
        console.print("  2. Check for case sensitivity issues in the ID")
    
    if not local_tutorial and not api_tutorial:
        console.print("[red]Tutorial not found locally or in API.[/red]")
        console.print("  1. Verify that the tutorial ID exists in your GitHub repository")
        console.print("  2. Check if there's a typo in the tutorial ID")
        console.print("  3. Create the tutorial file if it doesn't exist")


def main():
    """Main entry point for the diagnostic script."""
    console.rule("[bold]CmdShiftLearn Tutorial Diagnostics[/bold]")
    
    # Authenticate first
    console.print("Authenticating with API...")
    success, api_key, error = login()
    
    if not success or not api_key:
        console.print("[red]Authentication failed. Please set up your API key.[/red]")
        if error:
            console.print(f"[red]{error}[/red]")
        return
    
    console.print("[green]Authentication successful![/green]")
    
    # Run diagnostics
    local_tutorials = check_local_tutorials()
    api_tutorials = check_api_tutorials(api_key)
    compare_tutorials(local_tutorials, api_tutorials)
    
    # Check if specific tutorial was provided
    if len(sys.argv) > 1:
        tutorial_id = sys.argv[1]
        console.print(f"\n[bold]Running diagnostics for specific tutorial: {tutorial_id}[/bold]")
        diagnose_specific_tutorial(api_key, tutorial_id)
    
    console.rule("[bold]Diagnostics Complete[/bold]")


if __name__ == "__main__":
    main()