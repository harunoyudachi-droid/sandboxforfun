const readline = require('node:readline/promises');
const { stdin: input, stdout: output } = require('node:process');

function calculate(left, operator, right) {
  if (!Number.isFinite(left) || !Number.isFinite(right)) {
    throw new Error('Введите конечные числа.');
  }
  let result;
  switch (operator) {
    case '+': result = left + right; break;
    case '-': result = left - right; break;
    case '*': result = left * right; break;
    case '/':
      if (right === 0) throw new Error('Делить на ноль нельзя.');
      result = left / right;
      break;
    default: throw new Error('Допустимые операции: +, -, *, /.');
  }
  if (!Number.isFinite(result)) throw new Error('Результат выходит за диапазон чисел.');
  return result;
}

function parseNumber(value) {
  if (value.trim() === '') throw new Error('Число не может быть пустым.');
  return Number(value.trim().replace(',', '.'));
}

async function main() {
  const rl = readline.createInterface({ input, output });
  try {
    console.log('Простой калькулятор');
    const left = parseNumber(await rl.question('Первое число: '));
    const operator = (await rl.question('Операция (+, -, *, /): ')).trim();
    const right = parseNumber(await rl.question('Второе число: '));
    console.log(`Результат: ${calculate(left, operator, right)}`);
  } catch (error) {
    console.error(`Ошибка: ${error.message}`);
    process.exitCode = 1;
  } finally {
    rl.close();
  }
}

if (require.main === module) main();
module.exports = { calculate, parseNumber };
