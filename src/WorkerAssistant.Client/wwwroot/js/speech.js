const synth = window.speechSynthesis;

function speak(text, lang = "en-US") {
    if (!synth) {
        console.error("SpeechSynthesis not supported");
        return;
    }

    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = lang; // Set language (e.g., "en-US" or "ru-RU")

    // Find a voice for the language
    const voices = synth.getVoices();
    const voice = voices.find(v => v.lang.startsWith(lang));
    if (voice) utterance.voice = voice;

    synth.speak(utterance);
}

// Get available voices
function getVoices() {
    return new Promise((resolve) => {
        const voices = synth.getVoices();
        if (voices.length > 0) {
            resolve(voices.map(v => v.name));
        } else {
            synth.onvoiceschanged = () => {
                resolve(synth.getVoices().map(v => v.name));
            };
        }
    });
}

// Expose to Blazor
window.speak = speak;
window.getVoices = getVoices;