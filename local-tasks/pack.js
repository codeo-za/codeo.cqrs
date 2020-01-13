const gulp = requireModule("gulp-with-help"),
  packageDir = require("./config").packageDir,
  runSequence = requireModule("run-sequence"),
  isPackMasterBranch = require("./modules/is-pack-master-branch"),
  findLocalNuget = requireModule("find-local-nuget"),
  git = require("simple-git/promise"),
  spawn = requireModule("spawn");

gulp.task("prepare-pack", done => {
  runSequence(
    "build-for-release",
    "increment-package-version",
    done);
});

gulp.task("pack", ["prepare-pack"], () => {
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
  var args = ["pack", "src/Codeo.CQRS/Package.nuspec", "-OutputDirectory", packageDir];
  const onPackMasterBranch = await isPackMasterBranch();
  if (!onPackMasterBranch) {
    var sha = await readCurrentShortSha();
    var suffix = `b${timestamp()}-${sha}`;
    args.push("-Suffix");
    args.push(suffix);
  }
  const nuget = await findLocalNuget();
  return spawn(nuget, args);
}
