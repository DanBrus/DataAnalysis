# DataAnalysis
Проект по обработке экспериментальных данных, согласно курсу "Методы обработки и анализа экспериментальных данных"

Данный проект является программой, предоставляющей функционал по обработке и анализу данных в графическом виде. Реализованы возможности:
- Загрузки данных формата jpeg и bmp, а так же сохранения данных в формате jpeg.
- Сохранения и загрузки файлов с расширением .dat с использованием внутреннего алгоритма
- Простой обработки данных, такой как поэлементное умножение данных на определённое число,
- Градационных преобразований данных на основе пользовательской модели данных
- Изменения размера изображения методами билинейной интерполяции, интерполяции по ближ. соседу и методом передискретизации ряда Фурье,
- Наложения шумов на имеющиеся данные и фильтрации данных методом скользящего окна
- Конволюции и Деконволюции (обратная фильтрация) изображения
- Изменения глубины цвета изображения, а так же порогового преобразования яркости
- Анализа гистограммы изображения
- Прямого и обратного преобразования Фурье
- Применения к изображению фильтров низких и высоких частот, а также полосового фильтра,
- Морфолигической обработки изображений: эрозии, заполнения и т.п.
- Поиска и подсчёта объектов на изображении при помощи алгоритма Хафа

Качество кода в данной работе оставляет желать лучшего: существует множество багов и внутренних косяков в парограмме (например, при печати данных в виде изображения на экране могут искажаться данные если их значения были больше 225 или меньше 0)
Программа работает корректно только с ЧБ изображениями, хоте потенциально может работать и с цветными.

Возможно, когда-нибудь я приведу ЭТО в порядочный вид, а пока пусть лежит тут.
