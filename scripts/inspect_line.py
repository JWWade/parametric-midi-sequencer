import sys
path = r'c:\Users\josh\source\personal-projects\parametric-midi-sequencer\tests\HarmonyGeneratorTests.cs'
with open(path, 'r', encoding='utf-8') as f:
    lines = f.readlines()
line = lines[33]
print(repr(line))
for idx, ch in enumerate(line):
    print(idx, ord(ch), repr(ch))
