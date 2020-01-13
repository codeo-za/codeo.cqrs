const gulp = requireModule("gulp-with-help"),
  packageDir = require("./config").packageDir,
  runSequence = requireModule("run-sequence"),
  isPackMasterBranch = require("./modules/is-pack-master-branch"),
  findLocalNuget = requireModule("find-local-nuget"),
  git = require("simple-git/promise"),
  spawn = requireModule("spawn");

gulp.task("prepare-pack", done => {
  runSequence(
    "increment-package-version",
    done);
});

// TODO: convert to using 'pack' from gulp-tasks
gulp.task("pack", ["prepare-pack"], () => {
  return doPack();
});

gulp.task("quick-pack", () => {
  return doPack();
});

async function readCurrentShortSha() {
  return await git().revparse([ "--short", "HEAD" ]);
}

function timestamp() {
  var d = new Date();
  return `${
    zeroPad(d.getFullYear() % 100)
  }${
    zeroPad(d.getMonth() + 1) // js months start at zero :/
  }${
    zeroPad(d.getDate())
  }${
    zeroPad(d.getHours())
  }${
    zeroPad(d.getMinutes())
  }`;
}

function zeroPad(num) {
  return num < 10 ? `0${num}`: num.toString();
}

async function doPack() {
  var args = ["pack", "src/Codeo.CQRS/Codeo.CQRS.csproj", "-o", packageDir];
  const onPackMasterBranch = await isPackMasterBranch();
  if (!onPackMasterBranch) {
    var sha = await readCurrentShortSha();
    var suffix = `b${timestamp()}-${sha}`;
    args.push("--version-suffix");
    args.push(suffix);
  }
  return spawn("dotnet", args);
}
