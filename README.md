# Стартовый проект Godot
<img width="1900" height="1025" alt="image" src="https://github.com/user-attachments/assets/20574ada-bb7a-4976-9b42-b4f2c7d26f78" />

# Стек: Godot v4.6 с поддержкой .NET
### Что имеется:
## 1. Контроллер 3D персонажа:

   ### 1.1. Передвижение (ходьба/бег/прыжки)
   
   ### 1.2. Рэйтрейсинг для взаимодействия с миром
   
   ### 1.3. Эффекты камеры при движении 
   
   ### 1.4. Стэйт машина
   
<img width="331" height="401" alt="image" src="https://github.com/user-attachments/assets/57f79190-982d-4e4e-925b-98f1b2ea2e2a" />

## 2. Плагины:

   ### 2.1. Asset Placer - помогает расставлять объекты на сцене
   
   <img width="1423" height="632" alt="image" src="https://github.com/user-attachments/assets/fe9f5d4c-51e0-4b76-9f51-8c1a2544d532" />

   ### 2.2. ShaderLib - доступ к библиотеке шейдеров прямо в движке
   
   <img width="1227" height="769" alt="image" src="https://github.com/user-attachments/assets/7d4a2738-d571-4f51-a5f6-cf084aec429e" />

   ### 2.3. StateChart - стейт машина для логики игрока и нпс

   ### 2.4. Todo Manager - штука для глобальных комментов в коде

## 3. Пост-обработка:

   ### 3.1. 3 вида готовых WorldEnvironment в папке Assets -> Environment
   
   <img width="326" height="563" alt="image" src="https://github.com/user-attachments/assets/ccf11bdf-79c6-491d-871b-5899363bb7f2" />

   ### 3.2. 1 пикселизирующий шейдер в папке shaders

## 4. Архитектура:  (Будет дописываться)

   ### 4.1. Код игрока реализован с помощью стейт машины и классов-компонентов, которые могут быть прикреплины к любой подходящий для них ноде, что делает их универсальными.
        Все важные параметры компонентов экспортируются в редактор, что делает тестирование игры проще.
        
   <img width="323" height="266" alt="image" src="https://github.com/user-attachments/assets/863ef323-534b-4bde-9a27-61c465b08c56" />

   ### 4.2. Главная сцена:
   
   <img width="339" height="207" alt="image" src="https://github.com/user-attachments/assets/1414a863-34a8-4862-bd81-f8b6d217c059" />

   ### 4.3. ...





