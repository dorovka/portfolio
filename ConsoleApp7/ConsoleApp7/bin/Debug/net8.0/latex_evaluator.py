
import sys
import json
from latex2sympy import latex2sympy

def evaluate_latex(latex_expr):
    try:
        expr = latex2sympy(latex_expr)
        result = float(expr.evalf())
        return {'success': True, 'result': result, 'expression': str(expr)}
    except Exception as e:
        return {'success': False, 'error': str(e)}

if __name__ == "__main__":
    # Читаем входные данные из stdin вместо argv
    input_data = sys.stdin.read()
    data = json.loads(input_data)
    latex_expression = data['expression']
    output = evaluate_latex(latex_expression)
    print(json.dumps(output))
