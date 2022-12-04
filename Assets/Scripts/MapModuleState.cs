using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class MapModuleState
{
    /// <summary>
    /// Шаблон, соответствующий текущему MapModuleState
    /// </summary>
    public MapModule Prefab { get; private set; }
    /// <summary>
    /// Поворот шаблона 
    /// </summary>
    public Vector3 Rotation { get; private set; }
    /// <summary>
    /// Контакты шаблона и их направления с учетом поворота
    /// </summary>
    public Dictionary<Vector2, MapModuleContact> Contacts { get; private set; }

    public MapModuleState(MapModule prefab, Vector3 rotation)
    {
        Prefab = prefab;
        Rotation = rotation;
        Contacts = new Dictionary<Vector2, MapModuleContact>();
    }

    /// <summary>
    /// Проверка на возможность соединение текущего варианта модуля (состояния) с другим. Так, к примеру, если direction указывает вправо
    /// будет выполнена проверка на возможность соединения правого контакта текущего состояния, с левым контактом состояния otherModuleState.
    /// </summary>
    /// <param name="otherModuleState">модуль, возможность соединения с которым надо проверить</param>
    /// <param name="direction">направление по которому расположен на карте otherModuleState относительно текущего</param>
    /// <returns>возвращает true, если модули можно соединить и false в противном случае</returns>
    public bool IsMatchingModules(MapModuleState otherModuleState, Vector2 direction)
    {
        (var current, var other) = GetConnectedContacts(otherModuleState, direction);
        return current.IsMatchingContacts(other);
    }
    /// <summary>
    /// Возвращает пару контактов для соединения текущего состояния с другим. Так если direction указывает вправо
    /// будет возвращен правый контакт текущего состояния и левый контакт состояния otherModuleState
    /// </summary>
    /// <param name="otherModuleState"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public (MapModuleContact Current, MapModuleContact Other) GetConnectedContacts(MapModuleState otherModuleState, Vector2 direction)
    {
        var currentContact = Contacts[direction];
        var otherContact = otherModuleState.Contacts[-direction];
        return (currentContact, otherContact);
    }

    /// <summary>
    /// Инстанцирует шаблон с поворотом, соответствующий состоянию на сцену как дочерний элемент map.
    /// </summary>
    /// <param name="map">Карта в которой надо инстанцировать шаблон</param>
    /// <param name="localPosition">Позиция шаблона</param>
    public void InstantiatePrefab(Map map, Vector3 localPosition)
    {
        var GO = MonoBehaviour.Instantiate(Prefab);
        GO.transform.parent = map.transform;
        GO.transform.localPosition = localPosition;
        GO.transform.Rotate(Rotation);
    }
}

