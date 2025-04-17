import {xu} from "xu";
import {cmdUtil, runUtil, sysUtil} from "xutil";
import {path} from "std";
import {XLog} from "xlog";

// This is deno JS code which calls ueListFiles and uerip to extract data from PAK files
// YOU DO NOT need to use this. I recommend just examining what it does below and re-writing it in YOUR LANGUAGE OF CHOICE
// If you do want to run this directly, you'll need to checkout this repo too: https://github.com/Sembiance/deno
// Since that's not ideal, I HIGHLY recommend just writing your own code to run the ueListFiles/uerip built binaries directly
const xlog = new XLog();

const argv = cmdUtil.cmdInit({
	cmdid   : "uerip",
	version : "1.0.0",
	desc    : "Rips all assets from any PAK files found in pakDir to outDir",
	opts    :
	{
		debug         : {desc : "Enable debug mode"},
		aesKey        : {desc : "AES Key", hasValue : true, required : true, asString : true},
		pakDir        : {desc : "PAK directory path ", hasValue : true, required : true},
		outDir        : {desc : "Output directory path", hasValue : true, required : true},
		ueVersion     : {desc : "Unreal Engine version to target", hasValue : true, required : false},
		mapFile       : {desc : "Path to a map file to use", hasValue : true, required : false},
		maxDuration   : {desc : "Maximum duration in ms per file extraction", hasValue : true, default : xu.MINUTE*5},
		omitSubstring : {desc : "A substring to match and omit certain fileids from extraction.", hasValue : true, multiple : true},
		onlyInclude   : {desc : "Only include the specified substrings as part of extraction", hasValue : true, multiple : true},
		skipSlow      : {desc : "Skip slow processing (meshes, animations, skeletons)."}
	}});

const standardArgs = ["--pakDir", argv.pakDir, "--aesKey", argv.aesKey];
if(argv.ueVersion)
	standardArgs.push("--ueVersion", argv.ueVersion);

// First get a list of all our fileids. This is because CUE4Parse has a tendency to crash so we need to rip files one at a time
const {stdout : fileidsRaw} = await runUtil.run(path.join(import.meta.dirname, "ueListFiles/bin/Release/net8.0/publish/ueListFiles"), standardArgs, {inheritEnv : true});

// Now remove the extensions and only get unique fileids
const fileids = fileidsRaw.trim().split("\n").map(v => path.join(path.dirname(v), path.basename(v, path.extname(v)))).unique().sortMulti();
fileids.filterInPlace(fileied => !(argv.omitSubstring || []).some(omitSubstring => fileied.includes(omitSubstring)));
fileids.filterInPlace(fileied => !argv.onlyInclude || argv.onlyInclude.some(omitSubstring => fileied.includes(omitSubstring)));

if(argv.debug)
	xlog.info`${fileids.join("\n")}`;

// Now we can extract all the files
xlog.info`Extracting ${fileids.length} files...`;
await fileids.parallelMap(async fileid =>
{
	await runUtil.run(path.join(import.meta.dirname, "uerip/bin/Release/net8.0/publish/uerip"), [...standardArgs, "--outDir", argv.outDir, ...(argv.skipSlow ? ["--skipSlow"] : []), ...(argv.mapFile ? ["--mapFile", argv.mapFile] : []), "--fileid", fileid], {inheritEnv : true, timeout : argv.maxDuration, liveOutput : true});
}, argv.debug ? 1 : await sysUtil.calcMaxProcs(undefined, {expectedMemoryUsage : 8*xu.GB}));
