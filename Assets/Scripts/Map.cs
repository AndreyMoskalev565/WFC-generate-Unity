using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Map : MonoBehaviour
{
    /// <summary>
    /// Размер карты, _mapSize.x - количество модулей вдоль оси X, а _mapSize.y - количество модулей вдоль оси Z
    /// </summary>
    [SerializeField] Vector2Int _mapSize = new Vector2Int(5, 5);
    /// <summary>
    /// Длинна одной стороны квадратной ячейки карты 
    /// </summary>
    [SerializeField] float _cellSize;
    /// <summary>
    /// Набор шаблонов для генерации
    /// </summary>
    [SerializeField] MapModule[] _mapModules;
    /// <summary>
    /// Список типов контактов, представленных объектами MapModuleContact, который заполняется в инспекторе.
    /// Инциализирован как список экземпляра класса object, так-как ссылочная сериализация в Unity поддерживает только
    /// такой формат списка. Для заполнения этого списка экземплярами класса MapModuleContact используется метод OnValidate.
    /// </summary>
    [SerializeReference] List<object> _contactTypes = new List<object>();
    /// <summary>
    /// Двумерный массив объектов MapCell, представляющий собой карту. Проводя аналогию с таблицей в этом массиве можно условно выделить
    /// строки (измерение массива с индексом 0) и столбцы (измерение массива с индексом 1).
    /// </summary>
    public MapCell[,] MapCellsMatrix;
    /// <summary>
    /// Количество строк в MapCellsMatrix
    /// </summary>
    public int RowsCount => MapCellsMatrix.GetLength(0);
    /// <summary>
    /// Количество столбцов в MapCellsMatrix
    /// </summary>
    public int ColumnsCount => MapCellsMatrix.GetLength(1);
    /// <summary>
    /// Одномерная версия двумерного массива MapCellsMatrix. Позволяет использовать функции Linq для работы с ячейками карты
    /// </summary>
    private MapCell[] _mapCellsArray;

    /// <summary>
    /// Этот метод запускается всякий раз при изменении параметров в Unspector-e. Используется для динамического заполнения
    /// _contactTypes объектами 
    /// </summary>
    private void OnValidate()
    {
        for (int i = 0; i < _contactTypes.Count; i++)
        {
            if (_contactTypes[i] as MapModuleContact == null) _contactTypes[i] = new MapModuleContact();
        }
    }

    /// <summary>
    /// Генерация запускается в начале игры и состоит из 3-ех этапов:
    /// InizializeMap() - заполнение MapCellsMatrix объектами типа MapCell
    /// FillCells() - определение конкретных состояний для ячеек с помощью алгоритма WFC
    /// CreateMap() - создание сгенерированной карты на сцене
    /// </summary>
    private void Start()
    {
        InizializeMap();
        FillCells();
        CreateMap();
    }

    /// <summary>
    /// Метод для инициализации пустой карты в виде двумерного массива MapCellsMatrix и его одномерной версии - _mapCellsArray
    /// </summary>
    void InizializeMap()
    {
        // инициализация двумерного массива, представляющего карту
        MapCellsMatrix = new MapCell[_mapSize.x, _mapSize.y];

        // получение всех возможных вариантов шаблонов (состояний ячеек)
        var mapModules = GetMapModules();
        /* MapCellsMatrix заполняется объектами MapCell. В конструктор каждого такого объекта
         * передаются следующие аргументы:
         * this - ссылка на текущий экземпляр Map
         * new Vector2Int(i, j) - позиция ячейки в MapCellsMatrix, в виде структуры Vector2Int
         * new List<MapModuleState>(mapModules) - копия списка со всеми возможными вариантами состояний для ячеек.
         * (важно передавать именно копию, а не ссылку, т. к. для каждой ячейки список возможных состояний должен быть индивидуальным) */
        for (int i = 0; i < _mapSize.x; i++)
        {
            for (int j = 0; j < _mapSize.y; j++)
                MapCellsMatrix[i, j] = new MapCell(this, new Vector2Int(i, j), new List<MapModuleState>(mapModules));
        }
        // инициализируем _mapCellsArray, путем преобразования MapCellsMatrix в одномерный массив
        _mapCellsArray = MapCellsMatrix.Cast<MapCell>().ToArray();
    }

    /// <summary>
    /// Определяет конкретные состояния для ячеек карты, представленных объектами MapCell в MapCellsMatrix, с помощью алгоритма WFC
    /// </summary>
    void FillCells()
    {
        MapCell cell = null;
        /* Цикл, реализующий определение состояний для ячеек карты. Выполняется пока в карте есть ячейки с более чем одним возможным состоянием.
         * В обратном случае произойдет возврат из метода. Также цикл прекратится, если метод cell.TrySelectState вернет false. Это означает, 
         * что для одного или нескольких модулей были заданы некорректные ограничения, в следствие чего карту невозможно сгенерировать.
         */

        // выполняется определение состояния для ячейки и последующее волновое обновление с помощью метода cell.TrySelectState (принцип его работы см. в описании к методу)
        do
        {
            // получаем массив ячеек для которых существует более одного возможного состояния
            var cellsWithUnselectedState = _mapCellsArray.Where(c => c.States.Count > 1).ToArray();

            // отсутствие в cellsWithUnselectedState элементов означает, что состояния всех ячеек были успешно определены.
            // Поэтому выполняем возврат из метода и переходим к следующему этапу
            if (cellsWithUnselectedState.Length == 0)
                return;

            // При выборе ячейки для определения состояния, приоритет отдается ячейкам, имеющим минимальное количество состояний, но не менее двух.
            // Это уменьшает вероятность неудачного обновления. В строке ниже мы узнаем минимальное количество состояний.
            var minStatesCount = cellsWithUnselectedState.Min(c => c.States.Count);

            // Находим ячейку с минимальным количеством состояний
            cell = cellsWithUnselectedState.First(c => c.States.Count == minStatesCount);
        }
        while (cell.TrySelectState(states => states[Random.Range(0, states.Count)]));
    }

    /// <summary>
    /// Реализует инстанцирование модулей, соответсвующих состояниям каждой ячейки карты
    /// </summary>
    void CreateMap()
    {
        for (int i = 0; i < _mapSize.x; i++)
        {
            for (int j = 0; j < _mapSize.y; j++)
            {
                // Получаем локальную позицию модуля
                var localPosition = new Vector3(i * _cellSize, 0, j * _cellSize);
                // На данном этапе для каждой ячейки в карте может соответствовать только одно состояние,
                // поэтому мы просто получаем его по индексу 0 и с помощью метода InstantiatePrefab
                // инстанцируем соответствующий состоянию вариант модуля на сцене
                MapCellsMatrix[i, j].States[0].InstantiatePrefab(this, localPosition);
            }
        }
    }

    /// <summary>
    /// Возвращает список всех возможных состояний (вариантов модулей) соответсвующих заданным в инспекторе шаблонам
    /// </summary>
    /// <returns></returns>
    List<MapModuleState> GetMapModules()
    {
        List<MapModuleState> mapModules = new List<MapModuleState>();
        foreach (var module in _mapModules)
        {
            // получаем список вариантов конкретного модуля с помощью метода module.GetMapModulesFromPrefab()
            // и добавляем его элементы в результирующий список mapModules
            mapModules.AddRange(module.GetMapModulesFromPrefab());
        }     
        return mapModules;
    }

    /// <summary>
    /// Возваращает объект MapModuleContact, соответсвующий заданному типу контакта.
    /// </summary>
    /// <param name="contactType">Тип контакта</param>
    /// <returns></returns>
    public MapModuleContact GetContact(string contactType)
    {
        return _contactTypes.First(c => c is MapModuleContact contact && contact.ContactType == contactType) as MapModuleContact;
    }
}