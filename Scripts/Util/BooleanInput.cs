using System;
using Godot;

namespace TspSolver.Util;

public class BooleanInput
{
	private bool _hasDefaultValue;
	private bool _defaultValue;
	private bool _value;

	public bool Value
	{
		get => _value;
		set
		{
			if (_value == value)
			{
				return;
			}

			_value = value;
			ValueChanged?.Invoke(value);
		}
	}

	public event Action<bool> ValueChanged;

	public BooleanInput() { }

	public BooleanInput(Action<bool> onChange)
	{
		this.ValueChanged += onChange;
	}

	public BooleanInput(bool defaultValue, Action<bool> onChange = null)
	{
		this._defaultValue = defaultValue;
		this._value = defaultValue;
		this._hasDefaultValue = true;

		if (onChange != null)
		{
			ValueChanged += onChange;
		}
	}

	public void Reset()
	{
		Value = _defaultValue;
	}

	public BooleanInput Bind(BaseButton checkButton)
	{
		checkButton.Toggled += newValue => this.Value = newValue;
		this.ValueChanged += newValue => checkButton.ButtonPressed = newValue;

		if (!_hasDefaultValue)
		{
			_defaultValue = checkButton.ButtonPressed;
			_hasDefaultValue = true;
			Value = _defaultValue;
		}
		else
		{
			checkButton.ButtonPressed = _defaultValue;
		}

		return this;
	}
}