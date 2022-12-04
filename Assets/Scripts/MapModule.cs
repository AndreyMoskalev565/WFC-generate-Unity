using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapModule : MonoBehaviour
{
    /// <summary>
    /// Объект Map, в котором используется модуль
    /// </summary>
    [SerializeField] Map _map;
    /// <summary>
    /// Тип конаткта, направленного вдоль оси Z
    /// </summary>
    [SerializeField] string _forwardContactType;
    /// <summary>
    /// Тип конаткта, направленного вдоль оси -Z
    /// </summary>
    [SerializeField] string _backContactType;
    /// <summary>
    /// Тип конаткта, направленного вдоль оси X
    /// </summary>
    [SerializeField] string _rightContactType;
    /// <summary>
    /// Тип конаткта, направленного вдоль оси -X
    /// </summary>
    [SerializeField] string _leftContactType;

    /// <summary>
    /// Массив с типами контактов шаблона, элементы которого расположены в порядке поворота по часовой стрелке.
    /// Тоесть если мы повернем шаблон на 90 гардусов, то его forward contact будет направлен в ту сторону,
    /// в которую ранее был направлен right contact. Таким образом, после _forwardContactType будет находится 
    /// _rightContactType.
    /// </summary>
    string[] _contactTypes => new string[]
    {
        _forwardContactType,
        _rightContactType,
        _backContactType,
        _leftContactType
    };

    /// <summary>
    /// Массив направлений для контактов оносительно карты, элементы которого расположены в порядке поворота по часовой стрелке.
    /// </summary>
    Vector2[] _contactDirections => new Vector2[]
    {
        ContactDirectionInMap.Forward,
        ContactDirectionInMap.Right,
        ContactDirectionInMap.Back,
        ContactDirectionInMap.Left
    };

    /// <summary>
    /// Генерирует список объектов MapModuleState, представляющих собой разные варианты поворота шаблона вокруг оси Y
    /// </summary>
    /// <returns>список вариантов шаблона с разным поворотом вокруг оси Y</returns>
    public List<MapModuleState> GetMapModulesFromPrefab()
    {
        var contactTypes = _contactTypes;
        var contactDirections = _contactDirections;
        List<MapModuleState> mapModules = new List<MapModuleState>();
        var rotationY = 0;
        // каждая итерация цикла i, представляет собой один поворот шаблона на -90 градусов
        for (int i = 0; i < contactDirections.Length; i++)
        {
            MapModuleState module = new MapModuleState(this, Vector3.up * rotationY);
            // В цикле j реализуется симуляция поворота объекта. Так если при первой итерации цикла i, направлению forward будет соответствовать контакт
            // с типом _forwardContactType, то при второй итерации, этому же направлению будет соответсвовать контакт с типом _rightContactType, 
            // при третьей - _backContactType, а при четвертой - _leftContactType.

            // Полученные пары направление-контакт сохраняются в module.Contacts
            for (int j = 0; j < contactTypes.Length; j++)
            {
                var typeIndex = (i + j) % contactTypes.Length;
                var contact = _map.GetContact(contactTypes[typeIndex]);
                module.Contacts.Add(contactDirections[j], contact);
            }
            // добавляем сгенерированный вариант в результирующий список
            mapModules.Add(module);
            // условно поворачиваем объект на 90 градусов
            rotationY -= 90;
        }
        return mapModules;
    }
}
