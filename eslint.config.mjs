import {default as denoConfig} from "/mnt/compendium/DevLab/common/eslint/deno.eslint.config.js";

denoConfig.push({
	ignores :
	[
		"sandbox/"
	]
});

export default denoConfig;	// eslint-disable-line unicorn/prefer-export-from
