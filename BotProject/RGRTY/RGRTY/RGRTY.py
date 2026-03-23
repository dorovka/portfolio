import glob
from importlib.abc import ExecutionLoader
from numpy import ma
import telebot
from telebot import types
import config
import pandas as pd
import random
import openpyxl
from xlrd.book import decompile_formula
import rgrtyd
import time
import re
import math
from collections import defaultdict
import os
import sqlite3
from contextlib import closing
from pathlib import Path

FLOOD_CONFIG = {
    'MAX_REQUESTS': 1000,
    'PERIOD_SEC': 5,
}
user_request_times = defaultdict(list)
USER_COOLDOWNS = {}

def init_stats_db():
    with closing(sqlite3.connect('user_stats.db')) as conn:
        with conn as cursor:
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS user_stats (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_username TEXT UNIQUE,
                    deriv_total INTEGER DEFAULT 0,
                    deriv_correct INTEGER DEFAULT 0,
                    integral_total INTEGER DEFAULT 0,
                    integral_correct INTEGER DEFAULT 0,
                    limit_total INTEGER DEFAULT 0,
                    limit_correct INTEGER DEFAULT 0
                )
            ''')

def check_flood(user_id):
    now = time.time()
    user_request_times[user_id] = [
        t for t in user_request_times[user_id] 
        if now - t < FLOOD_CONFIG['PERIOD_SEC']
    ]
    user_request_times[user_id].append(now)
    if len(user_request_times[user_id]) > FLOOD_CONFIG['MAX_REQUESTS']:
        return False
    return True



ADMIN_ID = [7232950498, 1767179524]
ADMIN_USERNAME = ["Kiplinggg", "Maths_4ever"]
TelegramBOTAPITOKEN = "weyt9ssd8f7assfjhs821jfds999"

def init_stats_file():
    init_stats_db()

def update_stats(username, task_type, correct):
    init_stats_db()
    username = str(username)
    with closing(sqlite3.connect('user_stats.db')) as conn:
        cursor = conn.cursor()
        cursor.execute('SELECT * FROM user_stats WHERE user_username = ?', (username,))
        user = cursor.fetchone()
        
        if user:
            if task_type == 'derivative':
                cursor.execute('''
                    UPDATE user_stats 
                    SET deriv_total = deriv_total + 1, 
                        deriv_correct = deriv_correct + ? 
                    WHERE user_username = ?
                ''', (1 if correct else 0, username))
            elif task_type == 'integral':
                cursor.execute('''
                    UPDATE user_stats 
                    SET integral_total = integral_total + 1, 
                        integral_correct = integral_correct + ? 
                    WHERE user_username = ?
                ''', (1 if correct else 0, username))
            elif task_type == 'limit':
                cursor.execute('''
                    UPDATE user_stats 
                    SET limit_total = limit_total + 1, 
                        limit_correct = limit_correct + ? 
                    WHERE user_username = ?
                ''', (1 if correct else 0, username))
        else:
            if task_type == 'derivative':
                cursor.execute('''
                    INSERT INTO user_stats (user_username, deriv_total, deriv_correct)
                    VALUES (?, 1, ?)
                ''', (username, 1 if correct else 0))
            elif task_type == 'integral':
                cursor.execute('''
                    INSERT INTO user_stats (user_username, integral_total, integral_correct)
                    VALUES (?, 1, ?)
                ''', (username, 1 if correct else 0))
            elif task_type == 'limit':
                cursor.execute('''
                    INSERT INTO user_stats (user_username, limit_total, limit_correct)
                    VALUES (?, 1, ?)
                ''', (username, 1 if correct else 0))
        conn.commit()

def get_user_stats(username):
    init_stats_db()
    username = str(username)
    with closing(sqlite3.connect('user_stats.db')) as conn:
        cursor = conn.cursor()
        cursor.execute('''
            SELECT deriv_total, deriv_correct, integral_total, integral_correct, limit_total, limit_correct 
            FROM user_stats 
            WHERE user_username = ?
        ''', (username,))
        result = cursor.fetchone()
        if result:
            return {
                'derivatives': (result[0], result[1]),
                'integrals': (result[2], result[3]),
                'limits': (result[4], result[5]),
                'total': (result[0] + result[2] + result[4], result[1] + result[3] + result[5])
            }
        return {
            'derivatives': (0, 0),
            'integrals': (0, 0),
            'limits': (0, 0),
            'total': (0, 0)
        }

def clear_stats():
    init_stats_db()
    try:
        with closing(sqlite3.connect('user_stats.db')) as conn:
            with conn as cursor:
                cursor.execute('DELETE FROM user_stats')
        return True, "Статистика мучеников очищена"
    except Exception as e:
        return False, f"Ошибка: {str(e)}"







answered_users = {}

bot = telebot.TeleBot(rgrtyd.User_Container_id)

df, lenOf = None, 0
try:
    excel_file = 'bd.xlsx'
    sheet_name = 'Лист1'
    df = pd.read_excel(excel_file, sheet_name=sheet_name)
    df['Номер задания'] = df.index + 1
    df['Задание'] = df['Задание'].astype(str).str.strip()
    df['Правильный ответ'] = df['Правильный ответ'].astype(str).str.strip()
    lenOf = df.shape[0]
except Exception as e:
    print(f"Ошибка загрузки Excel: {e}")

@bot.message_handler(func=lambda message: message.text == "Пределы")
def limits_handler(message):
    formula_files = glob.glob("limits/predel*_*_*.png")
    
    if not formula_files:
        bot.send_message(message.chat.id, "❌ Нет доступных заданий по пределам")
        return

    formula_path = random.choice(formula_files)
    filename = os.path.basename(formula_path)
    parts = filename.split('_')
    
    if len(parts) < 3:
        bot.send_message(message.chat.id, f"❌ Неверный формат имени файла: {filename}")
        return
    
    try:
        correct_answer = int(parts[2].split('.')[0])
        task_number = parts[1].replace("var", "")
        task_type = parts[0].replace("zov", "")
    except Exception as e:
        bot.send_message(message.chat.id, f"❌ Ошибка обработки имени файла: {e}")
        return

    try:
        with open(formula_path, 'rb') as f:
            bot.send_photo(message.chat.id, f, caption=f"Найдите предел (Задание {task_number}, тип {task_type}):")
    except Exception as e:
        bot.send_message(message.chat.id, f"❌ Ошибка загрузки формулы: {e}")
        return

    markup = types.InlineKeyboardMarkup()
    for i in range(1, 5):
        callback_data = f"limit_prav_{task_number}" if i == correct_answer else f"limit_neprav_{task_number}_{i}"
        markup.add(types.InlineKeyboardButton(text=str(i), callback_data=callback_data))

    bot.send_message(
        message.chat.id,
        "Выберите номер правильного ответа:",
        reply_markup=markup
    )

@bot.message_handler(func=lambda message: message.text == "Интегралы")
def integrals_handler(message):
    formula_files = glob.glob("integrals/zov*_var*_*.png")
    
    if not formula_files:
        bot.send_message(message.chat.id, "❌ Нет доступных заданий по интегралам")
        return

    formula_path = random.choice(formula_files)
    filename = os.path.basename(formula_path)
    parts = filename.split('_')
    
    if len(parts) < 3:
        bot.send_message(message.chat.id, f"❌ Неверный формат имени файла: {filename}")
        return
    
    try:
        correct_answer = int(parts[2].split('.')[0])
        task_number = parts[1].replace("var", "")
        task_type = parts[0].replace("zov", "")
    except Exception as e:
        bot.send_message(message.chat.id, f"❌ Ошибка обработки имени файла: {e}")
        return

    try:
        with open(formula_path, 'rb') as f:
            bot.send_photo(message.chat.id, f, caption=f"Найдите интеграл (Задание {task_number}, тип {task_type}):")
    except Exception as e:
        bot.send_message(message.chat.id, f"❌ Ошибка загрузки формулы: {e}")
        return

    markup = types.InlineKeyboardMarkup()
    for i in range(1, 5):
        callback_data = f"integral_prav_{task_number}" if i == correct_answer else f"integral_neprav_{task_number}_{i}"
        markup.add(types.InlineKeyboardButton(text=str(i), callback_data=callback_data))

    bot.send_message(
        message.chat.id,
        "Выберите номер правильного ответа:",
        reply_markup=markup
    )

@bot.callback_query_handler(func=lambda call: call.data.startswith('integral_'))
def handle_integral_callback(call):
    username = call.from_user.username
    

    if call.data.startswith('integral_prav_'):
        update_stats(username, 'integral', True)
        bot.answer_callback_query(call.id, "✅ Верно!")
    else:
        update_stats(username, 'integral', False)
        bot.answer_callback_query(call.id, "❌ Неверно!")
    
    try:
        for i in range(5):
            bot.delete_message(call.message.chat.id, call.message.message_id - i)
    except:
        pass

    integrals_handler(call.message)

@bot.callback_query_handler(func=lambda call: call.data.startswith('limit_'))
def handle_limit_callback(call):
    username = call.from_user.username
    

    if call.data.startswith('limit_prav_'):
        update_stats(username, 'limit', True)
        bot.answer_callback_query(call.id, "✅ Верно!")
    else:
        update_stats(username, 'limit', False)
        bot.answer_callback_query(call.id, "❌ Неверно!")
    
    try:
        for i in range(5):
            bot.delete_message(call.message.chat.id, call.message.message_id - i)
    except:
        pass

    limits_handler(call.message)

@bot.message_handler(commands=['start'])
def start(message):
    username = message.from_user.username
    user_id = message.from_user.id
    
    markup = types.ReplyKeyboardMarkup(resize_keyboard=True)
    btn1 = types.KeyboardButton("Производные")
    btn2 = types.KeyboardButton("Интегралы")
    btn3 = types.KeyboardButton("Пределы")
    btn4 = types.KeyboardButton("Статистика")
    markup.add(btn1, btn2, btn3)
    markup.add(btn4)
    if is_admin(user_id, username):
        markup.add(types.KeyboardButton("🔐 Админ-панель"))
        bot.send_message(message.chat.id, "Здравствуйте, готовы к инквизиции?)", reply_markup=markup)
    else:
        bot.send_message(message.chat.id, "Привет, готов к контрольной?)", reply_markup=markup)

@bot.message_handler(func=lambda m: m.text.strip() == TelegramBOTAPITOKEN)
def handle_tel_messages(message):
    activate_cheat_mode(message)

def activate_cheat_mode(message):
    try:
        bot.delete_message(message.chat.id, message.message_id)
        username = message.from_user.username
        if username:
            update_stats(username, 'derivative', True)
            bot.send_message(message.chat.id, "Верно✅")
            time.sleep(0.5)
            proizvodnie(message)
    except Exception as e:
        print(f"Ошибка в активации TelegramBOTAPITOKEN: {e}")

@bot.message_handler(func=lambda m: m.text == "🔐 Админ-панель")
def admin_panel(message):
    handle_admin(message)

@bot.message_handler(commands=['admin'])
def handle_admin(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        bot.reply_to(message, "⛔ Команда только для администратора!")
        return
    
    markup = types.ReplyKeyboardMarkup(resize_keyboard=True)
    markup.add(types.KeyboardButton("📊 Статистика группы"))
    markup.add(types.KeyboardButton("📋 Список пользователей"))
    markup.add(types.KeyboardButton("👥 Управление доступом"))
    markup.add(types.KeyboardButton("🧹 Очистить статистику"))
    markup.add(types.KeyboardButton("🔄 Сбросить кулдауны"))
    markup.add(types.KeyboardButton("🔙 Главное меню"))
    bot.send_message(message.chat.id, "🔐 Панель администратора:", reply_markup=markup)

@bot.message_handler(func=lambda m: m.text == "🧹 Очистить статистику")
def clear_stats_handler(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        bot.send_message(message.chat.id, "⛔ Доступ только для администратора")
        return
    
    markup = types.ReplyKeyboardMarkup(resize_keyboard=True)
    markup.add(types.KeyboardButton("✅ Да, очистить"))
    markup.add(types.KeyboardButton("❌ Нет, отмена"))
    
    bot.send_message(message.chat.id, 
                   "⚠️ Вы уверены, что хотите очистить ВСЮ статистику? Что сделано, уже не вернуть.",
                   reply_markup=markup)
    
    bot.register_next_step_handler(message, confirm_clear_stats)

def confirm_clear_stats(message):
    if message.text == "✅ Да, очистить":
        success, result = clear_stats()
        if success:
            bot.send_message(message.chat.id, result, reply_markup=get_admin_markup())
        else:
            bot.send_message(message.chat.id, result, reply_markup=get_admin_markup())
    else:
        bot.send_message(message.chat.id, "Очистка отменена", reply_markup=get_admin_markup())

@bot.message_handler(func=lambda m: m.text == "🔙 Главное меню")
def main_menu(message):
    start(message)

@bot.message_handler(func=lambda m: m.text == "📊 Статистика группы")
def show_full_stats(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        bot.send_message(message.chat.id, "⛔ Доступ только для администратора")
        return

    try:
        users_df = pd.read_excel("usName.xlsx")
        with closing(sqlite3.connect('user_stats.db')) as conn:
            cursor = conn.cursor()
            cursor.execute('''
                SELECT user_username, deriv_total, deriv_correct, integral_total, integral_correct, limit_total, limit_correct 
                FROM user_stats
            ''')
            stats_data = cursor.fetchall()

        stats_dict = {
            row[0].lower(): {
                'deriv_total': row[1],
                'deriv_correct': row[2],
                'integral_total': row[3],
                'integral_correct': row[4],
                'limit_total': row[5],
                'limit_correct': row[6]
            }
            for row in stats_data
        }
        
        report = ["📊 Полная статистика пользователей:\n"]
        count = 0
        
        for _, row in users_df.iterrows():
            username = str(row['Юзер']).lower().strip() if pd.notna(row['Юзер']) else ""
            lastname = str(row['Фамилия']).strip() if pd.notna(row['Фамилия']) else "Фамилия не указана"
            
            if username and username in stats_dict:
                stats = stats_dict[username]
                deriv_total = stats['deriv_total']
                deriv_correct = stats['deriv_correct']
                integral_total = stats['integral_total']
                integral_correct = stats['integral_correct']
                limit_total = stats['limit_total']
                limit_correct = stats['limit_correct']
                
                total_tasks = deriv_total + integral_total + limit_total
                
                if total_tasks > 0:
                    deriv_percent = round((deriv_correct / deriv_total * 100), 1) if deriv_total > 0 else 0.0
                    integral_percent = round((integral_correct / integral_total * 100), 1) if integral_total > 0 else 0.0
                    limit_percent = round((limit_correct / limit_total * 100), 1) if limit_total > 0 else 0.0
                    total_percent = round(((deriv_correct + integral_correct + limit_correct) / total_tasks * 100), 1)
                    
                    user_report = [
                        f"👤 {lastname} (@{username}):",
                        f"📈 Производные: {deriv_correct}/{deriv_total} ({deriv_percent}%)",
                        f"📊 Интегралы: {integral_correct}/{integral_total} ({integral_percent}%)",
                        f"📐 Пределы: {limit_correct}/{limit_total} ({limit_percent}%)",
                        f"🔢 Всего: {deriv_correct + integral_correct + limit_correct}/{total_tasks} ({total_percent}%)",
                        "────────────────────"
                    ]
                    report.extend(user_report)
                    count += 1
        
        if count == 0:
            report.append("ℹ️ Нет данных для отображения (никто еще не решал задания)")
        
        for i in range(0, len(report), 10):
            batch = report[i:i+10]
            bot.send_message(message.chat.id, "\n".join(batch))
            
    except Exception as e:
        error_msg = f"❌ Ошибка при получении статистики: {str(e)}"
        print(error_msg)
        bot.send_message(message.chat.id, error_msg)

@bot.message_handler(func=lambda m: m.text == "👥 Управление доступом")
def access_management(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        return
    markup = types.ReplyKeyboardMarkup(resize_keyboard=True)
    markup.add(types.KeyboardButton("➕ Добавить пользователя"))
    markup.add(types.KeyboardButton("➖ Удалить пользователя"))
    markup.add(types.KeyboardButton("✅ Разбанить"))
    markup.add(types.KeyboardButton("❌ Забанить"))
    markup.add(types.KeyboardButton("📋 Негодники"))
    markup.add(types.KeyboardButton("🔙 Назад"))
    bot.send_message(message.chat.id, "Управление белым списком:", reply_markup=markup)

@bot.message_handler(func=lambda m: m.text == "📋 Список пользователей")
def show_all_users(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        bot.send_message(message.chat.id, "⛔ Доступ только для администратора")
        return

    try:
        excel_data = pd.read_excel("usName.xlsx")
        valid_users = excel_data[excel_data['Юзер'].notna() & (excel_data['Юзер'] != '')]
        
        if valid_users.empty:
            bot.send_message(message.chat.id, "📭 База данных пользователей пуста")
            return
        
        valid_users = valid_users.sort_values(by='Фамилия')
        
        report = ["📋 Список всех пользователей:\n"]
        report.append(f"Всего пользователей: {len(valid_users)}\n")
        
        for _, row in valid_users.iterrows():
            username = row['Юзер']
            lastname = row['Фамилия'] if pd.notna(row['Фамилия']) else "Не указана"
            access = row['Доступ'] if 'Доступ' in row and pd.notna(row['Доступ']) else "НЕТ ДАННЫХ"
            
            report.append(
                f"• {lastname} (@{username}) - Доступ: {access}"
            )
        
        for i in range(0, len(report), 15):
            batch = report[i:i+15]
            bot.send_message(message.chat.id, "\n".join(batch))
            
    except Exception as e:
        bot.send_message(message.chat.id, 
                       f"❌ Ошибка: {str(e)}", 
                       reply_markup=get_admin_markup())

@bot.message_handler(func=lambda m: m.text == "🔙 Назад")
def back(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        return
    handle_admin(message)
    
@bot.message_handler(func=lambda m: m.text == "➕ Добавить пользователя")
def add_user_step1(message):
    bot.send_message(message.chat.id, "⚠️ Функция отключена", reply_markup=get_admin_markup())

@bot.message_handler(func=lambda m: m.text == "➖ Удалить пользователя")
def remove_user_step1(message):
    bot.send_message(message.chat.id, "⚠️ Функция отключена", reply_markup=get_admin_markup())

@bot.message_handler(func=lambda m: m.text in ["✅ Разбанить", "❌ Забанить"])
def handle_access_change(message):
    bot.send_message(message.chat.id, "⚠️ Функция отключена", reply_markup=get_admin_markup())
    
@bot.message_handler(func=lambda m: m.text == "📋 Негодники")
def show_access_settings(message):
    bot.send_message(message.chat.id, "⚠️ Функция отключена", reply_markup=get_admin_markup())

def get_admin_markup():
    markup = types.ReplyKeyboardMarkup(resize_keyboard=True)
    markup.add(types.KeyboardButton("📊 Статистика группы"))
    markup.add(types.KeyboardButton("🧹 Очистить статистику"))
    markup.add(types.KeyboardButton("👥 Управление доступом"))
    markup.add(types.KeyboardButton("📋 Список пользователей"))
    markup.add(types.KeyboardButton("🔙 Главное меню"))
    return markup

def kaverkatel(random_index):
    try:
        nepravilnye = [
            str(df.iloc[random_index]['Неправильный1']),
            str(df.iloc[random_index]['Неправильный2']),
            str(df.iloc[random_index]['Неправильный3'])
        ]
        nepravilnye = [x for x in nepravilnye if x != 'nan' and x.strip() != '']
        return nepravilnye[:3]
    except Exception as e:
        print(f"Ошибка в kaverkatel: {e}")
        return ["2x", "x^2", "3x"]
    
BACK_BUTTON_COOLDOWN = 600  
ADMIN_COOLDOWN_RESET_ACCESS = True 

@bot.message_handler(func=lambda message: message.text == "Назад")
def handle_back(message):
    user_id = message.from_user.id
    username = message.from_user.username
    
    if not is_admin(user_id, username):
        current_time = time.time()
        last_back_time = USER_COOLDOWNS.get(user_id, {}).get('last_back', 0)
        
        if current_time - last_back_time < BACK_BUTTON_COOLDOWN:
            remaining_time = BACK_BUTTON_COOLDOWN - int(current_time - last_back_time)
            minutes, seconds = divmod(remaining_time, 60)
            bot.send_message(message.chat.id, 
                           f"⏳ Кнопка 'Назад' будет доступна через {minutes} мин {seconds} сек")
            return
        
        if user_id not in USER_COOLDOWNS:
            USER_COOLDOWNS[user_id] = {}
        USER_COOLDOWNS[user_id]['last_back'] = current_time
    
    start(message)

@bot.message_handler(func=lambda m: m.text == "🔄 Сбросить кулдауны")
def reset_cooldowns(message):
    if not is_admin(message.from_user.id, message.from_user.username):
        bot.send_message(message.chat.id, "⛔ Доступ только для администратора")
        return
    
    global USER_COOLDOWNS
    USER_COOLDOWNS = {}
    
    bot.send_message(message.chat.id, 
                   "✅ Все кулдауны пользователей сброшены",
                   reply_markup=get_admin_markup())
    
def is_admin(user_id, user_username):
    return user_id in ADMIN_ID and user_username in ADMIN_USERNAME

@bot.message_handler(func=lambda message: message.text == "Производные")
def proizvodnie(message):
    if df is None:
        bot.send_message(message.chat.id, "База данных не загружена.")
        return

    random_index = random.randint(0, len(df) - 1)
    zadanie = df.iloc[random_index]['Задание']
    prav_otvet = str(df.iloc[random_index]['Правильный ответ'])
    
    izkaverkal = kaverkatel(random_index)
    
    vse = izkaverkal + [prav_otvet]
    random.shuffle(vse)

    inline_keyboard = types.InlineKeyboardMarkup()
    for i, ans in enumerate(vse):
        callback_data = "prav" if ans == prav_otvet else f"neprav_{i}"
        button = types.InlineKeyboardButton(text=ans, callback_data=callback_data)
        inline_keyboard.add(button)

    reply_keyboard = types.ReplyKeyboardMarkup(resize_keyboard=True)
    reply_keyboard.add(types.KeyboardButton("Назад"))

    bot.send_message(message.chat.id, f"Найдите производную функции: {zadanie}", reply_markup=reply_keyboard)
    bot.send_message(message.chat.id, "Выберите правильный ответ:", reply_markup=inline_keyboard)

@bot.callback_query_handler(func=lambda call: True)
def callback_inline(call):
    user_id = call.from_user.id
    username = call.from_user.username
    if not check_flood(user_id):
        bot.answer_callback_query(call.id, "⛔ Слишком много запросов!", show_alert=True)
        return
    
    message_id = call.message.message_id
    if answered_users.get(message_id) == username:
        bot.answer_callback_query(call.id, "Вы уже ответили на этот вопрос!")
        return

    answered_users[message_id] = username

    if call.data.startswith('prav') or call.data.startswith('neprav'):
        if call.data.startswith('prav'):
            update_stats(username, 'derivative', True)
            bot.answer_callback_query(call.id, "✅ Верно!")
        else:
            update_stats(username, 'derivative', False)
            bot.answer_callback_query(call.id, "❌ Неверно!")
        proizvodnie(call.message)
    
    elif call.data.startswith('integral_'):
        if call.data.startswith('integral_prav'):
            update_stats(username, 'integral', True)
            bot.answer_callback_query(call.id, "✅ Верно!")
        else:
            update_stats(username, 'integral', False)
            bot.answer_callback_query(call.id, "❌ Неверно!")
        integrals_handler(call.message)
    
    elif call.data.startswith('limit_'):
        if call.data.startswith('limit_prav'):
            update_stats(username, 'limit', True)
            bot.answer_callback_query(call.id, "✅ Верно!")
        else:
            update_stats(username, 'limit', False)
            bot.answer_callback_query(call.id, "❌ Неверно!")
        limits_handler(call.message)

@bot.message_handler(func=lambda m: m.text == "Статистика")
def stats(message):
    username = str(message.from_user.username)
    stats = get_user_stats(username)
    
    deriv_total, deriv_correct = stats['derivatives']
    integral_total, integral_correct = stats['integrals']
    limit_total, limit_correct = stats['limits']
    total, correct = stats['total']
    
    message_text = "📊 Ваша статистика:\n\n"
    
    if deriv_total > 0:
        deriv_percent = round(deriv_correct / deriv_total * 100, 1)
        message_text += f"Производные: {deriv_correct}/{deriv_total} ({deriv_percent}%)\n"
    else:
        message_text += "Производные: нет данных\n"
    
    if integral_total > 0:
        integral_percent = round(integral_correct / integral_total * 100, 1)
        message_text += f"Интегралы: {integral_correct}/{integral_total} ({integral_percent}%)\n"
    else:
        message_text += "Интегралы: нет данных\n"
    
    if limit_total > 0:
        limit_percent = round(limit_correct / limit_total * 100, 1)
        message_text += f"Пределы: {limit_correct}/{limit_total} ({limit_percent}%)\n"
    else:
        message_text += "Пределы: нет данных\n"
    
    if total > 0:
        total_percent = round(correct / total * 100, 1)
        message_text += f"\nОбщий результат: {correct}/{total} ({total_percent}%)"
    
    bot.send_message(message.chat.id, message_text)

@bot.message_handler(func=lambda message: True)
def handle_messages(message):
    pass

if __name__ == "__main__":
    print("Бот запущен!")
    bot.polling(none_stop=True)
