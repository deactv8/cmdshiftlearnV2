
import httpx

challenge_id = "challenge_01_command_basics"
print(f"Testing connection to single challenge API for {challenge_id}...")
response = httpx.get(f'https://cmdshiftlearnv2.onrender.com/api/challenges/{challenge_id}')
print(f"Status code: {response.status_code}")
print("Response content:")
print(response.text[:1000])  # Print first 1000 chars to avoid overwhelming the output

# Try a different challenge ID
challenge_id = "filter_processes"
print(f"\nTesting connection to single challenge API for {challenge_id}...")
response = httpx.get(f'https://cmdshiftlearnv2.onrender.com/api/challenges/{challenge_id}')
print(f"Status code: {response.status_code}")
print("Response content:")
print(response.text[:1000])  # Print first 1000 chars to avoid overwhelming the output
