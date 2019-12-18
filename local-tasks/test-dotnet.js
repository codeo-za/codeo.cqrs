const
    spawn = requireModule("spawn"),
    path = require("path"),
    lsR = requireModule("ls-r"),
    gulp = requireModule("gulp-with-help");

gulp.task("test-dotnet-core", "Tests *.Tests.csproj with 'dotnet test'", ["build"], () => {
    return new Promise(async (resolve, reject) => {
        var projects = lsR(".").filter(p => {
            const filename = path.basename(p);
            return !!filename.match(/\.Tests\.csproj$/);
        }).map(p => p.match(/ /) ? `"${p}"` : p);
        if (projects.length === 0) {
            return reject("No test projects found");
        }
        for (const project of projects) {
            try {
                await spawn("dotnet", [ "test", project ]);
            } catch (e) {
                reject(e);
            }
        }
        resolve();
    });
});