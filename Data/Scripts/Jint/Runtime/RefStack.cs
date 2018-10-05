using System;
using System.Runtime.CompilerServices;
using Jint.Runtime.Environments;

namespace Jint.Runtime
{
    internal sealed class ExecutionContextStack
    {
        private ExecutionContext[] _array;
        private uint _size;

        private const int DefaultCapacity = 4;

        public ExecutionContextStack()
        {
            _array = new ExecutionContext[4];
            _size = 0;
        }

        public ExecutionContext Peek()
        {
            if (_size == 0)
            {
                ExceptionHelper.ThrowInvalidOperationException("stack is empty");
            }
            return _array[_size - 1];
        }

        public void Pop()
        {
            if (_size == 0)
            {
                ExceptionHelper.ThrowInvalidOperationException("stack is empty");
            }
            _size--;
        }

        public void Push(ExecutionContext item)
        {
            if (_size == (uint) _array.Length)
            {
                var newSize = 2 * _array.Length;
                var newArray = new ExecutionContext[newSize];
                Array.Copy(_array, 0, newArray, 0, _size);
                _array = newArray;
            }

            _array[_size++] = item;
        }

        public void ReplaceTopLexicalEnvironment(LexicalEnvironment newEnv)
        {
            _array[_size - 1] = _array[_size - 1].UpdateLexicalEnvironment(newEnv);
        }
    }
}