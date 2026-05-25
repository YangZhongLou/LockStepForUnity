using System;
using System.Collections.Generic;

namespace LockStepLib.Command
{
    /// <summary>
    /// 指令工厂。维护 CommandTypeId → 构造函数的映射，
    /// 供 CommandSerializer 在反序列化时按类型 ID 创建指令实例。
    /// </summary>
    public class CommandFactory
    {
        private readonly Dictionary<int, Func<IInputCommand>> _factories = new Dictionary<int, Func<IInputCommand>>();

        /// <summary>注册指令类型</summary>
        /// <param name="typeId">全局唯一类型 ID (0 保留)</param>
        /// <param name="factory">无参构造委托，返回新实例</param>
        public void Register(int typeId, Func<IInputCommand> factory)
        {
            if (typeId < 0)
                throw new ArgumentOutOfRangeException(nameof(typeId), "CommandTypeId 必须 >= 0");
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[typeId] = factory;
        }

        /// <summary>按类型 ID 创建指令实例</summary>
        public IInputCommand Create(int typeId)
        {
            if (_factories.TryGetValue(typeId, out var factory))
                return factory();
            throw new KeyNotFoundException($"未注册的指令类型 ID: {typeId}");
        }

        /// <summary>检查类型 ID 是否已注册</summary>
        public bool IsRegistered(int typeId) => _factories.ContainsKey(typeId);

        /// <summary>已注册类型数</summary>
        public int Count => _factories.Count;
    }
}
