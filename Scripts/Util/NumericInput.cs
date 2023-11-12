using System;
using Godot;

namespace TspSolver.Util;

public class NumericInput
{
	private bool _hasDefaultValue;
	private float _defaultValue;
	private float _value;

	public float Value
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

	public event Action<float> ValueChanged;

	public NumericInput() { }

	public NumericInput(Action<float> onChange)
	{
		this.ValueChanged += onChange;
	}

	public NumericInput(float defaultValue, Action<float> onChange = null)
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

	public NumericInput Bind(Slider slider)
	{
		slider.ValueChanged += newValue => this.Value = (float)newValue;
		this.ValueChanged += newValue => slider.Value = newValue;

		if (!_hasDefaultValue)
		{
			_defaultValue = (float)slider.Value;
			_hasDefaultValue = true;
			Value = _defaultValue;
		}
		else
		{
			slider.Value = _defaultValue;
		}

		return this;
	}

	public NumericInput Bind(SpinBox spinBox)
	{
		spinBox.ValueChanged += newValue => this.Value = (float)newValue;
		this.ValueChanged += newValue => spinBox.Value = newValue;

		if (!_hasDefaultValue)
		{
			_defaultValue = (float)spinBox.Value;
			_hasDefaultValue = true;
			Value = _defaultValue;
		}
		else
		{
			spinBox.Value = _defaultValue;
		}

		return this;
	}
}