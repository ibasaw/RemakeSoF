import re

NUM = r'[-+]?\d[\d.]*'

content = """
        start                   0.5176 0 0.05098 0.3176 0 0.02353       
"""

print("Testing 6-value regex:")
start_match = re.search(rf'start[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
if start_match:
    print("MATCHED 6 values:", [start_match.group(i) for i in range(1, 7)])
else:
    print("NO MATCH for 6 values")

print("\nTesting 3-value regex:")
m = re.search(rf'start[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
if m:
    print("MATCHED 3 values:", [m.group(i) for i in range(1, 4)])
else:
    print("NO MATCH for 3 values")

# Test with actual 0-only values
content2 = """
        start                   0.5176 0 0.05098 0.3176 0 0.02353       
"""
print("\nNUM pattern test on '0':")
print("Matches:", re.findall(NUM, "0"))
print("Matches on '0 0':", re.findall(NUM, "0 0"))

# The problem: NUM = r'[-+]?\d[\d.]*' - does "0" match?
print("\n'0' matches NUM:", bool(re.match(NUM, "0")))
# Yes, but between values there are spaces. Let me trace step by step.
print("\nFull line match test:")
line = "        start                   0.5176 0 0.05098 0.3176 0 0.02353"
all_nums = re.findall(NUM, line)
print("All numbers found:", all_nums)
