#!/usr/bin/env python3
"""
Run script for CmdShiftLearn PowerShell learning platform.
"""

import os
import sys
from app import CmdShiftLearn

def main():
    """Run the CmdShiftLearn application."""
    print("Starting CmdShiftLearn PowerShell learning platform...")
    
    try:
        app = CmdShiftLearn()
        app.start()
    except KeyboardInterrupt:
        print("\nExiting CmdShiftLearn...")
        sys.exit(0)
    except Exception as e:
        print(f"\nError: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()
