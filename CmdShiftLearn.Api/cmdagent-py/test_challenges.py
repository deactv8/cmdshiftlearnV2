
import httpx

print("Testing connection to challenges API...")
response = httpx.get('https://cmdshiftlearnv2.onrender.com/api/challenges')
print(f"Status code: {response.status_code}")
print("Response content:")
print(response.text[:1000])  # Print first 1000 chars to avoid overwhelming the output
