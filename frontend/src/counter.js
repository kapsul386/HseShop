// Демонстрационный код из шаблона Vite.
// В проекте не используется, можно оставить как есть.
export function setupCounter(element) {
    let counter = 0;

    const setCounter = (count) => {
        counter = count;
        element.innerHTML = `count is ${counter}`;
    };

    element.addEventListener("click", () => setCounter(counter + 1));
    setCounter(0);
}
