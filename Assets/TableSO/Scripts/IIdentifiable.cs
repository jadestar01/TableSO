using System;
using UnityEngine;

public interface IIdentifiable<T> where T : IConvertible
{
    public T ID { get; }
}
