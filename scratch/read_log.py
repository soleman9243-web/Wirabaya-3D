import json

log_path = r"C:\Users\USER\.gemini\antigravity-ide\brain\8ec212b0-47a6-4540-85c9-3abb785e94d8\.system_generated\logs\transcript.jsonl"
with open(log_path, "r", encoding="utf-8") as f:
    for line in f:
        d = json.loads(line)
        idx = d.get("step_index")
        if idx and 6160 <= idx <= 6192:
            print(f"[{idx}] {d.get('type')}: {str(d.get('content'))[:160]}")
            for c in d.get('tool_calls', []):
                print(f"   Call {c.get('name')}: {c.get('args')}")
