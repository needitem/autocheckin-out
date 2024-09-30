using System;
using System.Windows.Input;

namespace ClockOut.Helpers
{
    /// <summary>
    /// ICommand 구현체로, 액션과 실행 가능 여부를 바인딩할 수 있습니다.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// CanExecuteChanged 이벤트를 CommandManager.RequerySuggested와 연결합니다.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 실행할 액션과 실행 가능 여부를 결정하는 조건자를 받는 생성자.
        /// </summary>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 명령이 실행 가능한지 여부를 반환합니다.
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// 명령을 실행합니다.
        /// </summary>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// CanExecuteChanged 이벤트를 발생시켜 UI에 CanExecute 상태 변경을 알립니다.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}