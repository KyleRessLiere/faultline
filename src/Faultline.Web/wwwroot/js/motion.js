// Whether the browser is set to "reduce motion". Read once by the board animator: a player who has
// asked for less movement gets the outcome of a command straight away, with no slide and no flash.
// Nothing about the position depends on the answer — only how long it takes to appear.
window.faultlineMotion = {
    prefersReduced: function () {
        return !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
    }
};
