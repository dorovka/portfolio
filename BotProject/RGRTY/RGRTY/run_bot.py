import subprocess
import time
import sys
from datetime import datetime

SCRIPT = "RGRTY.py"
LOG_FILE = "crash_log.txt"

def log_error(error_text):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    with open(LOG_FILE, "a", encoding="utf-8") as f:
        f.write(f"[{timestamp}] {error_text}\n")
        f.write("-" * 50 + "\n")
    print(f"[{timestamp}] Ошибка в логах {LOG_FILE}")

while True:
    print(f"поехали {SCRIPT}...")
    
    try:
        process = subprocess.run(
            [sys.executable, SCRIPT],
            capture_output=True,
            text=True
        )
        
        if process.returncode == 0:
            break
        
        error_msg = process.stderr or "Неизвестная хуета"
        log_error(f"Код очка: {process.returncode}\n{error_msg}")
        print(f"[RUNNER] Бот упал, ты еблан.")
        time.sleep(5)
            
    except KeyboardInterrupt:
        print("сам сломал долбаеб")
        break
    except Exception as e:
        log_error(f"хуета: {e}")
        time.sleep(5)
