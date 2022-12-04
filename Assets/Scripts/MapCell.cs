using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapCell
{
    /// <summary>
    /// Позиция ячейки в карте
    /// </summary>
    public Vector2Int PositionInMap { get; private set; }
    /// <summary>
    /// Допустимые для ячейки состояния
    /// </summary>
    public List<MapModuleState> States { get; private set; }
    /// <summary>
    /// Позиции соседних ячеек в карте
    /// </summary>
    public List<Vector2Int> AdjacentCellsPositions { get; private set; }

    /// <summary>
    /// Карта, которой принадлежит ячейка
    /// </summary>
    private Map _map;

    /// <summary>
    /// Хранит набор допустимых состояний для текущей ячейки до волнового обновления, вызванного определением состояния другой ячейки.
    /// Используется для отката States в случае неудачного обновления.
    /// key - ячейка, определение состояния которой стало причиной волнового обновления.
    /// value - массив состояний текущей ячейки до конкретного обновления.
    /// </summary>
    private Dictionary<MapCell, MapModuleState[]> _mapCellCashe = new Dictionary<MapCell, MapModuleState[]>();

    public MapCell(Map map, Vector2Int positionInMap, List<MapModuleState> states)
    {
        States = states;
        PositionInMap = positionInMap;
        // получаем позиции соседних ячеек
        AdjacentCellsPositions = GetAdjacentCellsPositions(map);
        _map = map;
    }

    /// <summary>
    /// Возвращает список позиций в карте соседних ячеек для текущей
    /// </summary>
    /// <param name="map">Карта, которой принадлежит ячейка</param>
    /// <returns></returns>
    List<Vector2Int> GetAdjacentCellsPositions(Map map)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        if (PositionInMap.x - 1 >= 0) cells.Add(new Vector2Int(PositionInMap.x-1, PositionInMap.y));
        if (PositionInMap.x + 1 < map.RowsCount) cells.Add(new Vector2Int(PositionInMap.x+1, PositionInMap.y));
        if (PositionInMap.y - 1 >= 0) cells.Add(new Vector2Int(PositionInMap.x, PositionInMap.y-1));
        if (PositionInMap.y + 1 < map.ColumnsCount) cells.Add(new Vector2Int(PositionInMap.x, PositionInMap.y+1));
        return cells;
    }

    public delegate MapModuleState GetModuleAction(List<MapModuleState> modules);

    /// <summary>
    /// Реализует выбор конкретного состояния для ячейки и инициирует волновое обновление остальных ячеек.
    /// В случае если волновое обновление прошло неудачно, то есть для какой-то ячейки не осталось возможных состояний, откатывает все изменения связанные с текущим обновлением 
    /// и пробует определить другое состояние. 
    /// Если все доступные состояния для ячейки приводят к неудачному обновлению другой ячейки, возвращает false. В случае же успешного обновления возвращает true
    /// </summary>
    /// <param name="getModuleAction">функция выбора одного состояния из списка доступных состояний</param>
    /// <returns></returns>
    public bool TrySelectState(GetModuleAction getModuleAction)
    {
        // сохраняем States в кеше перед выбором состояния
        AddOrUpdateToMapCellCashe(this);
        // копируем список доступных состояний для ячейки.
        var states = new List<MapModuleState>(States);
        // цикл продолжается до тех пор, пока состояние для ячейки не будет успешно определено или пока 
        // в states есть состояния, которые еще не были попробованы
        while (states.Count > 0)
        {
            // выбираем состояние с помощью заданной функции
            var selectState = getModuleAction(states);
            // задаем новое состояние для ячейки
            States = new List<MapModuleState>() { selectState };
            // запускаем волновое обновление соседних ячеек
            if (!TryUpdateAdjacentCells(this))
            {
                // если в результате волнового обновления,для одной из ячеек не осталось допустимых состояний,
                // удаляем выбранное состояние из states и пробуем следующее
                states.Remove(selectState);
            }
            else return true;
        }
        return false;
    }

    delegate bool TryUpdateAction();
    /// <summary>
    /// Пробует обновить списки состояний соседних ячеек для текущей. Если в результате этой операции
    /// набор допустимых состояний некоторых ячеек изменился, инициирует аналогичное обновление для их соседних ячеек.
    /// Возвращает true в случае успешного обновления и false - если в результате обновления для какой-то из ячеек не осталось возможных состояний
    /// </summary>
    /// <param name="cellWithSelectedModule">Ячейка, вызов метода TrySelectState которой стал причиной волнового обновления</param>
    /// <returns></returns>
    bool TryUpdateAdjacentCells(MapCell cellWithSelectedModule)
    {
        // список операций TryUpdateAdjacentCells соседних ячеек. Заполняется в результате выполнения метода TryUpdatePossibleModules
        List<TryUpdateAction> updateAdjacentCellsActions = new List<TryUpdateAction>();
        // пробуем обновить множество состояний соседних ячеек
        bool updateSuccess = AdjacentCellsPositions.All(cellPos =>
        {
            return _map.MapCellsMatrix[cellPos.x, cellPos.y].TryUpdateStates(this, cellWithSelectedModule, updateAdjacentCellsActions);
        });
        if (!updateSuccess)
        {
            // если обновление состояний соседних ячеек завершилось неудачно, откатываем связанные с ним изменения с помощью метода ReverseAdjacentCells
            ReverseStates(cellWithSelectedModule);
            return false;
        }
        else
            // если соседние ячейки были успешно обновлены, при необходимости запускаем аналогичные обновления для их соседних ячеек.
            return updateAdjacentCellsActions.All(action => action.Invoke());
    }

    /// <summary>
    /// Обновляет States текущей ячейки в зависимости от States другой ячейки. Если в результате этого из States был удален один или несколько элементов,
    /// добавляет в updateAdjacentCellsActions операцию TryUpdateAdjacentCells, реализующую обновление соседних для текущей ячеек.
    /// </summary>
    /// <param name="otherCell">Ячейка, относительно которой будет обновлена текущая ячейка</param>
    /// <param name="cellWithSelectedState">Ячейка, вызов метода TrySelectState которой стал причиной волнового обновления</param>
    /// <param name="updateAdjacentCellsActions">список с последующими операциями обновления</param>
    /// <returns></returns>
    bool TryUpdateStates(MapCell otherCell, MapCell cellWithSelectedState, List<TryUpdateAction> updateAdjacentCellsActions)
    {
        // сохраняем States в кеш перед обновлением
        AddOrUpdateToMapCellCashe(cellWithSelectedState);

        // удаляем состояния, не сочетающиеся ни с одним состоянием ячейки otherCell
        int removeModuleCount = States.RemoveAll(thisState =>
        {
            // получаем информацию, о том в какой стороне на карте находится ячейка
            // относительно текущей ячейки в виде структуры Vector2Int: справа - (0,1), слева - (0,-1), спереди - (1,0) или сзади (-1,0).
            var directionToPreviusCell = otherCell.PositionInMap - PositionInMap;
            // проверяем, есть ли в otherCell.States, такое состояние, которое сочиталось бы с текущем состоянием thisState
            // при соеденении в заданном направлении. Например, это может быть проверка на возможность соеденения
            // левого контакта thisState с правым контактом otherState,
            // или верхнего контакта thisState с нижним контактом otherState и т. д.
            return !otherCell.States.Any(otherState => thisState.IsMatchingModules(otherState, directionToPreviusCell));
        });

        // если после обновления в States не осталось занчений, значит обновление прошло неудачно, возвращаем false
        if (States.Count == 0)
            return false;

        // если в результате обновления были удалены какие-то состояния, сохраняем функцию обновления соседних ячеек к текущей для вызова в будущем
        // этот момент можно было-бы упростить с помощью рекурсии, но я сделал так, чтобы реализация соответствовала приведенному в стаьте алгоритму.
        if (removeModuleCount > 0)
            updateAdjacentCellsActions.Add(() => TryUpdateAdjacentCells(cellWithSelectedState));

        return true;
    }

    /// <summary>
    /// Обновляет _mapCellCashe. Если значение с заданным key уже существует - перезаписывает его, если нет - добавляет новое значение
    /// </summary>
    /// <param name="originallyUpdatedCell">Ячейка, вызвавшая метод TrySelectState</param>
    void AddOrUpdateToMapCellCashe(MapCell originallyUpdatedCell)
    {
        if (_mapCellCashe.ContainsKey(originallyUpdatedCell)) _mapCellCashe[originallyUpdatedCell] = States.ToArray();
        else _mapCellCashe.Add(originallyUpdatedCell, States.ToArray());
    }

    /// <summary>
    /// Откатывает States всех ячеек начиная с текущей, затронутых волновным обновлением, которое началось в методе TrySelectState ячейки originallyUpdatedCell
    /// </summary>
    /// <param name="originallyUpdatedCell">Ячейка, вызвавшая метод TrySelectState</param>
    public void ReverseStates(MapCell originallyUpdatedCell)
    {
        if (_mapCellCashe.ContainsKey(originallyUpdatedCell))
        {
            States = new List<MapModuleState>(_mapCellCashe[originallyUpdatedCell]);
            _mapCellCashe.Remove(originallyUpdatedCell);
            foreach (var cellPos in AdjacentCellsPositions)
            {
                // вызывается метод ReverseStates для каждой соседней ячейки
                _map.MapCellsMatrix[cellPos.x, cellPos.y].ReverseStates(originallyUpdatedCell);
            }
        }
    }
}
