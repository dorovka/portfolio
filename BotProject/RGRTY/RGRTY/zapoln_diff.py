import pandas as pd
import numpy as np
import sympy

# Загружаем существующий файл Excel, если он есть, или создаем новый DataFrame
file_name = 'bd.xlsx'

try:
    # Попытка загрузить существующий файл
    df = pd.read_excel(file_name)
    # Если файл загружен, убедимся, что столбец 'Номер задания' существует
    if 'Номер задания' not in df.columns:
        df['Номер задания'] = range(1, len(df) + 1)  # Если нет, создаем и нумеруем
    df['Номер задания'] = df['Номер задания'].astype(str) #делаем строкой

except FileNotFoundError:
    # Если файл не найден, создаем новый DataFrame
    df = pd.DataFrame(columns=['Задание', 'Правильный ответ', 'Номер задания'])  # Создаем с заголовками

def vnosim(zadik, tema):
    global df  # Указываем, что используем глобальную переменную df
    #f(x) = cos(2x)
    chislo_random = np.random.randint(-100, 100, size=33)
    chislo_random = [np.random.randint(1,100) if x == 0 else x for x in chislo_random]
    for index in range(33):
        number = str(index + 1 + 33 * tema)

        zadik_custom = zadik.replace('A', str(chislo_random[index]))

        try:
            if 'X' in zadik_custom:
                # Определяем символьную переменную
                X = sympy.Symbol('X')

                # Определяем функцию
                f = eval(zadik_custom)
                f_diff = sympy.diff(f, X)
            else:
                f_diff = 0
            print(f_diff, zadik_custom, number)

            # Создаем словарь с данными для новой строки
            new_row = {'Задание': "f(x) =" + str(zadik_custom).replace('sympy.', '').replace('asin', 'arcsin').replace('acos','arccos').replace('sqrt', '√').replace('**','^'),
                       'Правильный ответ': str(f_diff).replace('sqrt', '√').replace('**','^'),
                       'Номер задания': number}
            
            # Проверяем, есть ли уже строка с таким номером
            if number in df['Номер задания'].values:
                # Если есть, обновляем значения
                df.loc[df['Номер задания'] == number, 'Задание'] = new_row['Задание']
                df.loc[df['Номер задания'] == number, 'Правильный ответ'] = new_row['Правильный ответ']
            else:
                # Если нет, добавляем новую строку
                df = pd.concat([df, pd.DataFrame([new_row])], ignore_index=True)

        except Exception as e:
            print(f"Ошибка вычисления: {e}, выражение: {zadik_custom}, номер: {number}")
            # Обрабатываем ошибки, устанавливая значения в None
            new_row = {'Задание': zadik_custom,
                       'Правильный ответ': None,
                       'Номер задания': number}
            
            if number in df['Номер задания'].values:
                # Если есть, обновляем значения на None
                df.loc[df['Номер задания'] == number, 'Задание'] = new_row['Задание']
                df.loc[df['Номер задания'] == number, 'Правильный ответ'] = new_row['Правильный ответ']
            else:
                # Если нет, добавляем новую строку с None
                df = pd.concat([df, pd.DataFrame([new_row])], ignore_index=True)


vnosim('sympy.cos(A * X)', 0) 
vnosim('sympy.log(A * X)', 1) 
vnosim('sympy.9*A', 2)
vnosim('sympy.asin(A * X)', 3)




# Сохраняем DataFrame в файл Excel
df.to_excel(file_name, index=False)

print(f"Файл '{file_name}' успешно обновлен!")