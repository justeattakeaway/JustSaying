using System;
using System.Collections.Generic;
using Rhino.Mocks;

namespace JustEat.Testing
{
	public class MockManager
	{
		private readonly IDictionary<Type, object> _mockDictionary;
	    private readonly MockRepository _mockRepository;

		public MockManager()
		{
            _mockRepository = new MockRepository();
			_mockDictionary = new Dictionary<Type, object>();
		}

		public T Mock<T>() where T : class
		{
			return (T)Mock(typeof (T));
		}

		public void Inject(object value)
		{
			_mockDictionary.Add(value.GetType(), value);
		}

		public object Mock(Type type)
        {
            var mock = _mockRepository.DynamicMock(type);
            _mockRepository.Replay(mock);

            if (!_mockDictionary.ContainsKey(type))
                _mockDictionary.Add(type, mock);

            return _mockDictionary[type];
		}

	}
}