module Plot.Core.Utils

let public generateRange(start: float, stop: float, step: float) : float seq =
    seq {start .. step .. stop}
